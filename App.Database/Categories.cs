namespace App.Database {
	using System;
	using System.ComponentModel.DataAnnotations;

	using App.Database.Models;

	using Dapper;

	public static class Categories {
		public static Category New(string name) {
			Validation.Validator.Validate(name, (ValidationAttribute[])typeof(Category).GetProperty(nameof(Category.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			return Database.Connection.QueryFirst<Category>(@"
				INSERT INTO categories (name) VALUES (@Name) RETURNING *;
				", new { Name = name });
		}

		public static Category? Get(int id) {
			return Database.Connection.QueryFirstOrDefault<Category>(@"
				SELECT * FROM categories WHERE id = @Id;
				", new { Id = id });
		}

		public static void Delete(int id) {
			int rowsAffected = Database.Connection.Execute(@"
				DELETE FROM categories WHERE id = @Id;
				", new { Id = id });

			if (rowsAffected == 0)
				throw new DatabaseException("Категория с таким ID не найдена.");
		}

		public static Category[] Query() {
			return Database.Connection.Query<Category>(@"SELECT * FROM categories;").ToArray();
		}
	}
}
