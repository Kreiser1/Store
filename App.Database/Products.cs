namespace App.Database {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;

	using Dapper;

	using Models;

	using Npgsql;

	public static class Products {
		public static Product New(string name, int count, int price, string? unit = null, string? description = null,
			int[]? categories = null, byte[]? image = null, string? manufacturer = null,
			string? provider = null, float? discount = null) {
			Validation.Validator.Validate(name, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (unit is not null)
				Validation.Validator.Validate(unit, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Unit)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (description is not null)
				Validation.Validator.Validate(description, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Description)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (manufacturer is not null)
				Validation.Validator.Validate(manufacturer, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Manufacturer)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (provider is not null)
				Validation.Validator.Validate(provider, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Provider)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (discount is not null)
				Validation.Validator.Validate(discount, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Discount)).GetCustomAttributes(typeof(ValidationAttribute), true));

			var product = Database.Connection.QueryFirst<Product>(@"
					INSERT INTO products (name, count, price, unit, description, image, manufacturer, provider, discount)
					VALUES (@Name, @Count, @Price, @Unit, @Description, @Image, @Manufacturer, @Provider, @Discount)
					RETURNING *;
					", new {
				Name = name, Count = count, Price = price, Unit = unit, Description = description,
				Image = image, Manufacturer = manufacturer, Provider = provider,
				Discount = discount
			});

			Validation.Validator.Validate(product);

			if (categories is not null)
				foreach (var category in categories)
					try {
						Database.Connection.Execute(@"
						INSERT INTO product_categories (product_id, category_id) VALUES (@ProductId, @CategoryId);
						", new { ProductId = product.Id, CategoryId = category });
					} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation) {
						throw new DatabaseException("Категории с таким ID не существует.");
					}

			product.Categories = categories;

			return product;
		}

		public static void Update(int id, string? name = null, int? count = null, int? price = null, string? unit = null,
			string? description = null, int[]? categories = null, byte[]? image = null, string? manufacturer = null,
			string? provider = null, float? discount = null) {

			if (name is not null)
				Validation.Validator.Validate(name, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (unit is not null)
				Validation.Validator.Validate(unit, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Unit)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (description is not null)
				Validation.Validator.Validate(description, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Description)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (manufacturer is not null)
				Validation.Validator.Validate(manufacturer, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Manufacturer)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (provider is not null)
				Validation.Validator.Validate(provider, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Provider)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (discount is not null)
				Validation.Validator.Validate(discount, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Discount)).GetCustomAttributes(typeof(ValidationAttribute), true));

			var clauses = new List<string>();
			var parameters = new DynamicParameters();
			parameters.Add("Id", id);

			if (name is not null) { clauses.Add("name = @Name"); parameters.Add("Name", name); }
			if (count is not null) { clauses.Add("count = @Count"); parameters.Add("Count", count); }
			if (price is not null) { clauses.Add("price = @Price"); parameters.Add("Price", price); }
			if (description is not null) { clauses.Add("description = @Description"); parameters.Add("Description", description); }
			if (image is not null) { clauses.Add("image = @Image"); parameters.Add("Image", image); }
			if (manufacturer is not null) { clauses.Add("manufacturer = @Manufacturer"); parameters.Add("Manufacturer", manufacturer); }
			if (provider is not null) { clauses.Add("provider = @Provider"); parameters.Add("Provider", provider); }
			if (discount is not null) { clauses.Add("discount = @Discount"); parameters.Add("Discount", discount); }

			using var transaction = Database.Connection.BeginTransaction();

			if (clauses.Count > 0) {
				int rowsAffected = Database.Connection.Execute($"UPDATE products SET {string.Join(", ", clauses)} WHERE id = @Id;", parameters, transaction);

				if (rowsAffected == 0) {
					transaction.Rollback();
					throw new DatabaseException("Продукт с таким ID не найден.");
				}
			}

			if (categories is not null) {
				Database.Connection.Execute("DELETE FROM product_categories WHERE product_id = @ProductId;", new { ProductId = id }, transaction);

				try {
					foreach (var category in categories)
						Database.Connection.Execute(@"
							INSERT INTO product_categories (product_id, category_id) 
							VALUES (@ProductId, @CategoryId);
							", new { ProductId = id, CategoryId = category }, transaction);
				} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation) {
					transaction.Rollback();
					throw new DatabaseException("Категории с таким ID не существует.");
				}
			}

			transaction.Commit();
		}

		public static void Delete(int id, bool force = false) {
			if (force)
				Database.Connection.Execute(@"
					DELETE FROM order_products WHERE product_id = @Id;
					", new { Id = id });

			try {
				int rowsAffected = Database.Connection.Execute(@"
					DELETE FROM products WHERE id = @Id;
					", new { Id = id });

				if (rowsAffected == 0)
					throw new DatabaseException("Продукт с таким ID не найден.");
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.RestrictViolation) {
				throw new DatabaseException("Продукт числится в заказах.");
			}
		}

		public static Category[] GetCategories(int id) {
			return Database.Connection.Query<Category>(@"
				SELECT categories.id, categories.name FROM product_categories
				JOIN categories ON categories.id = product_categories.category_id
				WHERE product_categories.product_id = @Id;",
				new { Id = id }).ToArray();
		}

		public static Product? Get(int id) {
			Product? product = Database.Connection.QueryFirstOrDefault<Product>(@"
				SELECT * FROM products WHERE id = @Id;
				", new { Id = id });

			if (product is null)
				return null;

			product.Categories = GetCategories(id).Select(category => category.Id).ToArray();

			return product;
		}

		public static Product[] Query(
			string? name = null,
			string? description = null,
			int[]? categories = null,
			string? manufacturer = null,
			string? provider = null,
			float? discount = null
		) {
			if (name is not null)
				Validation.Validator.Validate(name, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (description is not null)
				Validation.Validator.Validate(description, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Description)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (manufacturer is not null)
				Validation.Validator.Validate(manufacturer, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Manufacturer)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (provider is not null)
				Validation.Validator.Validate(provider, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Provider)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (discount is not null)
				Validation.Validator.Validate(discount, (ValidationAttribute[])typeof(Product).GetProperty(nameof(Product.Discount)).GetCustomAttributes(typeof(ValidationAttribute), true));

			var clauses = new List<string>();
			var parameters = new DynamicParameters();

			if (name is not null) {
				clauses.Add("products.name ILIKE @Name");
				parameters.Add("Name", $"%{name}%");
			}

			if (description is not null) {
				clauses.Add("products.description ILIKE @Description");
				parameters.Add("Description", $"%{description}%");
			}

			if (manufacturer is not null) {
				clauses.Add("products.manufacturer ILIKE @Manufacturer");
				parameters.Add("Manufacturer", $"%{manufacturer}%");
			}

			if (provider is not null) {
				clauses.Add("products.provider ILIKE @Provider");
				parameters.Add("Provider", $"%{provider}%");
			}

			if (discount is not null) {
				clauses.Add("products.discount >= @Discount");
				parameters.Add("Discount", discount);
			}

			if (categories is not null && categories.Length > 0) {
				clauses.Add(@"
					products.id IN (
					SELECT product_categories.product_id
					FROM product_categories
					WHERE product_categories.category_id = ANY(@Categories)
					GROUP BY product_categories.product_id
					HAVING COUNT(DISTINCT product_categories.category_id) = @Count
		        )");

				parameters.Add("Categories", categories);
				parameters.Add("Count", categories.Length);
			}

			string where = clauses.Count > 0 ? string.Join(" OR ", clauses) : "TRUE";

			return Database.Connection.Query<Product>($@"
				SELECT products.*, COALESCE(array_agg(category_id) FILTER (WHERE category_id IS NOT NULL), '{{}}')::int[] as categories
				FROM products
				LEFT JOIN product_categories ON products.id = product_categories.product_id
				WHERE {where}
				GROUP BY products.id;", parameters).ToArray();
		}

	}
}
