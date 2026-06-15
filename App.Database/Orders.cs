namespace App.Database {
	using System;
	using System.Collections.Generic;

	using App.Database.Models;

	using Dapper;

	using Npgsql;

	public static class Orders {
		public static Order New(int userId, (int Id, int Count)[] products) {
			try {
				using var connection = Database.Connection;

				Order order = connection.QueryFirst<Order>(@"
					INSERT INTO orders (user_id) VALUES (@UserId) RETURNING *;
					", new { UserId = userId });

				foreach (var (Id, Count) in products) {
					var product = Products.Get(Id);

					if (product is null)
						throw new DatabaseException("Продукт с таким ID не найден.");

					if (Count > product.Count)
						throw new DatabaseException("Такого количества продукта нет в наличии.");

					Products.Update(Id, count: product.Count - Count);

					connection.Execute(@"
						INSERT INTO order_products (order_id, product_id, count) VALUES (@OrderId, @ProductId, @Count);
						", new { OrderId = order.Id, ProductId = Id, Count = Count });
				}

				order.Products = products;
				return order;
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation) {
				throw new DatabaseException("Пользователь с таким ID не найден.");
			}
		}

		public static (Product Product, int Count)[] GetProducts(int id) {
			using var connection = Database.Connection;
			
			var products = connection.Query<(int, int)>(@"
				SELECT product_id, count FROM order_products WHERE order_id = @Id;
				", new { Id = id });

			if (products.Count() == 0)
				return [];

			List<(Product, int)> productList = new();

			foreach (var (productId, count) in products) {
				Product? product = Products.Get(productId);

				if (product is not null)
					productList.Add((product, count));
			}

			return productList.ToArray();
		}

		public static Order? Get(int id) {
			using var connection = Database.Connection;
			
			Order? order = connection.QueryFirstOrDefault<Order>(@"
					SELECT * FROM orders WHERE id = @Id;
				", new { Id = id });

			if (order is null)
				return null;

			order.Products = GetProducts(id).Select(product => (product.Product.Id, product.Count)).ToArray();

			return order;
		}

		public static void Update(int id, int? userId = null, (int Id, int Count)[]? products = null) {
			var clauses = new List<string>();
			var parameters = new DynamicParameters();

			parameters.Add("Id", id);

			if (userId is not null) {
				clauses.Add("user_id = @UserId");
				parameters.Add("UserId", userId);
			}

			using var connection = Database.Connection;
			using var transaction = connection.BeginTransaction();

			try {
				if (clauses.Count > 0) {
					int rowsAffected = connection.Execute($"UPDATE orders SET {string.Join(", ", clauses)} WHERE id = @Id;", parameters, transaction);

					if (rowsAffected == 0) {
						transaction.Rollback();
						throw new DatabaseException("Заказ с таким ID не найден.");
					}
				}

				if (products is not null) {
					connection.Execute("DELETE FROM order_products WHERE order_id = @OrderId;", new { OrderId = id }, transaction);

					foreach (var (productId, count) in products)
						connection.Execute(@"
							INSERT INTO order_products (order_id, product_id, count) 
							VALUES (@OrderId, @ProductId, @Count);
							", new { OrderId = id, ProductId = productId, Count = count }, transaction);
				}

				transaction.Commit();
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation) {
				throw new DatabaseException("Продукт с таким ID не найден.");
			}
		}

		public static void Delete(int id) {
			using var connection = Database.Connection;
			
			int rowsAffected = connection.Execute(@"
				DELETE FROM orders WHERE id = @Id;
				", new { Id = id });

			if (rowsAffected == 0)
				throw new DatabaseException("Заказ с таким ID не найден.");
		}

		public static Order[] Query(int? userId = null) {
			using var connection = Database.Connection;
		
			Order[] orders = connection.Query<Order>($@"
				SELECT * FROM orders {(userId is not null ? "WHERE user_id = @UserId" : "")};
				", userId is not null ? new { UserId = userId } : null).ToArray();

			if (orders.Count() == 0)
				return [];

			foreach (Order order in orders)
				order.Products = GetProducts(order.Id).Select(product => (product.Product.Id, product.Count)).ToArray();

			return orders;
		}
	}
}
