namespace App.Database.Models {
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Reflection;

	public class Category {
		public required int Id { get; set; }

		[StringLength(50, MinimumLength = 3, ErrorMessage = "Название категории должно быть от 3-х до 50-ти символов.")]
		public required string Name { get; set; }
	}

	public static class Role {
		public const string Client = "Client";
		public const string Manager = "Manager";
		public const string Admin = "Admin";
	};

	public class User {
		public required int Id { get; set; }

		[StringLength(50, MinimumLength = 3, ErrorMessage = "Длина логина должна быть от 3-х до 50-ти символов.")]
		public required string Login { get; set; }

		[StringLength(512, MinimumLength = 3, ErrorMessage = "Длина логина должна быть от 3-х до 512-ти символов.")]
		public required string Password { get; set; }

		[StringLength(100, MinimumLength = 3, ErrorMessage = "Длина имени пользователя должна быть от 3-х до 100 символов.")]
		public string? Name { get; set; }

		[AllowedValues(new object[] { Models.Role.Client, Models.Role.Manager, Models.Role.Admin }, ErrorMessage = "Неизвестная роль.")]
		public required string Role { get; set; }
	}

	public class Product {
		public required int Id { get; set; }

		[StringLength(100, MinimumLength = 3, ErrorMessage = "Длина названия продукта должна быть от 3-х до 100 символов.")]
		public required string Name { get; set; }

		public required int Count { get; set; }
		public required int Price { get; set; }
		
		[StringLength(50, MinimumLength = 3, ErrorMessage = "Длина единицы измерения должна быть от 3-х до 50-ти символов.")]
		public string? Unit { get; set; }
		
		public byte[]? Image { get; set; }

		[StringLength(1000, MinimumLength = 3, ErrorMessage = "Длина описания продукта должна быть от 3-х до 1000 символов.")]
		public string? Description { get; set; }

		[StringLength(50, MinimumLength = 3, ErrorMessage = "Длина названия поставщика должна быть от 3-х до 50-ти символов.")]
		public string? Provider { get; set; }

		[StringLength(50, MinimumLength = 3, ErrorMessage = "Длина названия производителя должна быть от 3-х до 50-ти символов.")]
		public string? Manufacturer { get; set; }

		[Range(0, 100, ErrorMessage = "Скидка должна быть от 0 до 100.")]
		public float? Discount { get; set; }
		public int[]? Categories { get; set; }
	}

	public class Order {
		public required int Id { get; set; }
		public required int UserId { get; set; }
		public required (int Id, int Count)[] Products { get; set; }
	}
}