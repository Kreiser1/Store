namespace App.Database {
	using System.Collections.Generic;

	using App.Database.Models;

	using Dapper;

	using BCrypt.Net;
	using System.ComponentModel.DataAnnotations;
	using Npgsql;

	public static class Users {
		public static User New(string login, string password, string role, string? name = null) {
			Validation.Validator.Validate(login, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Login)).GetCustomAttributes(typeof(ValidationAttribute), true));
			Validation.Validator.Validate(role, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Role)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (name is not null)
				Validation.Validator.Validate(name, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			password = BCrypt.HashPassword(password);

			Validation.Validator.Validate(password, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Password)).GetCustomAttributes(typeof(ValidationAttribute), true));

			try {
				using var connection = Database.Connection;

				var user = connection.QueryFirst<User>(@"
					INSERT INTO users (login, password, role, name) VALUES (@Login, @Password, @Role, @Name)
					RETURNING *;
					", new {
					Login = login,
					Password = password,
					Role = role,
					Name = name
				});

				Validation.Validator.Validate<User>(user);
				return user;
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation) {
				throw new DatabaseException("Пользователь с таким логином уже существует.");
			}
		}

		public static User? Login(string login, string password) {
			Validation.Validator.Validate(login, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Login)).GetCustomAttributes(typeof(ValidationAttribute), true));

			using var connection = Database.Connection;

			var user = connection.QueryFirstOrDefault<User>(@"
				SELECT * FROM users WHERE login = @Login;
				", new { Login = login });

			if (user is not null)
				if (!BCrypt.Verify(password, user.Password))
					user = null;

			return user;
		}

		public static void Update(int id, string? login = null, string? password = null, string? role = null, string? name = null) {
			if (login is not null)
				Validation.Validator.Validate(login, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Login)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (role is not null)
				Validation.Validator.Validate(role, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Role)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (name is not null)
				Validation.Validator.Validate(name, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (password is not null) {
				password = BCrypt.HashPassword(password);
				Validation.Validator.Validate(password, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Password)).GetCustomAttributes(typeof(ValidationAttribute), true));
			}

			var clauses = new List<string>();
			var parameters = new DynamicParameters();

			parameters.Add("Id", id);

			if (login is not null) {
				clauses.Add("login = @Login");
				parameters.Add("Login", login);
			}

			if (password is not null) {
				clauses.Add("password = @Password");
				parameters.Add("Password", password);
			}

			if (role is not null) {
				clauses.Add("role = @Role");
				parameters.Add("Role", role);
			}

			if (name is not null) {
				clauses.Add("name = @Name");
				parameters.Add("Name", name);
			}

			if (clauses.Count == 0)
				return;

			try {
				using var connection = Database.Connection;

				int rowsAffected = connection.Execute($"UPDATE users SET {string.Join(", ", clauses)} WHERE id = @Id;", parameters);

				if (rowsAffected == 0)
					throw new DatabaseException($"Пользователь с ID {id} не найден.");
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation) {
				throw new DatabaseException("Пользователь с таким логином уже существует.");
			}
		}

		public static User? Get(int id) {
			using var connection = Database.Connection;

			return connection.QueryFirstOrDefault<User>(@"
				SELECT * FROM users WHERE id = @Id;
				", new { Id = id });
		}

		public static void Delete(int id, bool force = false) {
			using var connection = Database.Connection;
			
			if (force)
				connection.Execute(@"
					DELETE FROM orders WHERE user_id = @Id;
					", new { Id = id });

			try {
				int rowsAffected = connection.Execute(@"
					DELETE FROM users WHERE id = @Id;
					", new { Id = id });

				if (rowsAffected == 0)
					throw new DatabaseException("Пользователь с таким ID не найден.");
			} catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.RestrictViolation) {
				throw new DatabaseException("У пользователя имеются заказы.");
			}
		}

		public static User[] Query(string? name = null, string? role = null) {
			if (name is not null)
				Validation.Validator.Validate(name, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Name)).GetCustomAttributes(typeof(ValidationAttribute), true));

			if (role is not null)
				Validation.Validator.Validate(role, (ValidationAttribute[])typeof(User).GetProperty(nameof(User.Role)).GetCustomAttributes(typeof(ValidationAttribute), true));

			var clauses = new List<string>();
			var parameters = new DynamicParameters();

			if (name is not null) {
				clauses.Add("name ILIKE @Name");
				parameters.Add("Name", $"%{name}%");
			}

			if (role is not null) {
				clauses.Add("role = @Role");
				parameters.Add("Role", role);
			}

			using var connection = Database.Connection;

			return connection.Query<User>($"SELECT * FROM users WHERE {(clauses.Count > 0 ? string.Join(" AND ", clauses) : "TRUE")};", parameters).ToArray();
		}
	}
}
