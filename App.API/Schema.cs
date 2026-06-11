using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(App.Database.Models.Role))]

namespace App.API.Schema;

public record LoginRequest(string Login, string Password);
public record LoginResponse(int Id, string Login, string Name, string Role, string Token);

public record CategoryResponse(int Id, string Name);
public record CategoryCreateRequest(string Name);

public record UserResponse(int Id, string Name, string Role);
public record UserCreateRequest(string Login, string Password, string Role, string? Name);
public record UserUpdateRequest(string? Login, string? NewPassword, string? OldPassword, string? Role, string? Name);
public record UserLoginRequest(string Login, string Password);

public record ProductResponse(
	int Id, string Name, int Count, int Price, string? Unit, byte[]? Image,
	string? Description, string? Manufacturer, string? Provider, float? Discount, CategoryResponse[]? Categories
);

public record ProductCreateRequest(
	string Name, int Count, int Price, string? Unit = null, string? Description = null,
	int[]? Categories = null, byte[]? Image = null, string? Manufacturer = null,
	string? Provider = null, float? Discount = null
);

public record ProductUpdateRequest(
	string? Name = null, int? Count = null, int? Price = null, string? Unit = null, string? Description = null,
	int[]? Categories = null, byte[]? Image = null, string? Manufacturer = null,
	string? Provider = null, float? Discount = null
);

public record ProductQueryRequest(
	string? Name = null, string? Description = null, int[]? Categories = null,
	string? Manufacturer = null, string? Provider = null, float? Discount = null
);

public record OrderResponse(int Id, int UserId, ProductResponse[] Products);
public record OrderCreateRequest(int UserId, (int Id, int Count)[] Products);
public record OrderUpdateRequest(int? UserId = null, (int Id, int Count)[]? Products = null);

public record ErrorResponse(string Message);