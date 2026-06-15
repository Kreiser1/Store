using App.Database;
using App.Database.Models;
using App.API.Schema;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;
using App.Validation;

namespace App.API;

internal static class API {
	private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(Role.Admin);
	private static int? GetUserId(ClaimsPrincipal user) {
		var claim = user.FindFirst(ClaimTypes.NameIdentifier);
		return claim is not null && int.TryParse(claim.Value, out int id) ? id : null;
	}

	public static void Initialize(WebApplication app) {
		app.MapPost("/api/auth/login", (LoginRequest request) => {
			try {
				var user = Users.Login(request.Login, request.Password);

				if (user is null)
					return Results.Unauthorized();

				var token = Auth.GenerateToken(user);

				return Results.Ok(new LoginResponse(user.Id, user.Login, user.Name ?? user.Login, user.Role, token));

			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapPost("/api/auth/register", (UserCreateRequest request) => {
			try {
				var user = Users.New(request.Login, request.Password, Role.Client, request.Name);
				var token = Auth.GenerateToken(user);
				return Results.Ok(new LoginResponse(user.Id, user.Login, user.Name ?? user.Login, user.Role, token));
			} catch (DatabaseException e) {
				return Results.Conflict(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapGet("/api/products", () => {
			var products = Products.Query();

			return Results.Ok(products.Select(product => new ProductResponse(
				product.Id, product.Name, product.Count, product.Price, product.Unit, product.Image,
				product.Description, product.Manufacturer, product.Provider, product.Discount,
				Products.GetCategories(product.Id).Select(category => new CategoryResponse(category.Id, category.Name)).ToArray()
			)));
		});

		app.MapGet("/api/categories", () => {
			var categories = Categories.Query();
			return Results.Ok(categories.Select(category => new CategoryResponse(category.Id, category.Name)));
		});

		app.MapPost("/api/categories", [Authorize(Roles = Role.Admin)] (CategoryCreateRequest request) => {
			try {
				var category = Categories.New(request.Name);
				return Results.Json(new CategoryResponse(category.Id, category.Name), statusCode: 201);
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapDelete("/api/categories/{id:int}", [Authorize(Roles = Role.Admin)] (int id) => {
			try {
				Categories.Delete(id);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.NotFound(new ErrorResponse(e.Message));
			}
		});

		app.MapGet("/api/users", [Authorize(Roles = Role.Admin)] (string? name, string? role) => {
			try {
				var users = Users.Query(name, role);
				return Results.Ok(users.Select(user => new UserResponse(user.Id, user.Name ?? user.Login, user.Role)));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapGet("/api/users/me", [Authorize] (ClaimsPrincipal user) => {
			var userId = GetUserId(user);

			if (userId is null)
				return Results.Unauthorized();

			var dbUser = Users.Get(userId ?? int.MinValue);

			return dbUser is null
				? Results.NotFound(new ErrorResponse("Пользователь не найден."))
				: Results.Ok(new UserResponse(dbUser.Id, dbUser.Name ?? dbUser.Login, dbUser.Role));
		});

		app.MapPatch("/api/users/me", [Authorize] (UserUpdateRequest request, ClaimsPrincipal user) => {
			var userId = GetUserId(user);

			if (userId is null)
				return Results.Unauthorized();

			var dbUser = Users.Get(userId ?? int.MinValue);

			if (dbUser is null)
				return Results.Unauthorized();

			if (request.Login != dbUser.Login || Users.Login(dbUser.Login, request.OldPassword ?? "") is null)
				return Results.Forbid();

			try {
				Users.Update(userId ?? int.MinValue, null, request.NewPassword, null, request.Name);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.Conflict(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapPatch("/api/users/{id:int}", [Authorize(Roles = Role.Admin)] (int id, UserUpdateRequest request) => {
			try {
				Users.Update(id, request.Login, request.NewPassword, request.Role, request.Name);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.Conflict(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapDelete("/api/users/{id:int}", [Authorize(Roles = Role.Admin)] (int id, bool force = false) => {
			try {
				Users.Delete(id, force);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.BadRequest(new ErrorResponse(e.Message));
			}
		});

		app.MapPost("/api/products", [Authorize(Roles = Role.Admin)] (ProductCreateRequest request) => {
			try {
				var product = Products.New(
					request.Name, request.Count, request.Price, request.Unit, request.Description,
					request.Categories, request.Image, request.Manufacturer, request.Provider, request.Discount
				);

				return Results.Json(new ProductResponse(
					product.Id, product.Name, product.Count, product.Price, product.Unit, product.Image,
					product.Description, product.Manufacturer, product.Provider, product.Discount,
					Products.GetCategories(product.Id).Select(category => new CategoryResponse(category.Id, category.Name)).ToArray()
				), statusCode: 201);
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapPatch("/api/products/{id:int}", [Authorize(Roles = Role.Admin)] (int id, ProductUpdateRequest request) => {
			try {
				Products.Update(id, request.Name, request.Count, request.Price, request.Unit, request.Description,
					request.Categories, request.Image, request.Manufacturer, request.Provider, request.Discount);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapDelete("/api/products/{id:int}", [Authorize(Roles = Role.Admin)] (int id, bool force = false) => {
			try {
				Products.Delete(id, force);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.BadRequest(new ErrorResponse(e.Message));
			}
		});

		app.MapPost("/api/products/query", [Authorize(Roles = $"{Role.Admin},{Role.Manager}")] (ProductQueryRequest request) => {
			try {
				var products = Products.Query(request.Name, request.Description, request.Categories,
					request.Manufacturer, request.Provider, request.Discount);

				return Results.Ok(products.Select(product => new ProductResponse(
					product.Id, product.Name, product.Count, product.Price, product.Unit, product.Image,
					product.Description, product.Manufacturer, product.Provider, product.Discount,
					Products.GetCategories(product.Id).Select(category => new CategoryResponse(category.Id, category.Name)).ToArray()
				)));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapGet("/api/orders", [Authorize(Roles = $"{Role.Admin},{Role.Manager}")] () => {
			try {
				var orders = Orders.Query();
					
				return Results.Json(orders.Select(order => new OrderResponse(order.Id, order.UserId, order.Products.Select((product) => {
					var _product = Products.Get(product.Id);

					return _product is null
						? new ProductResponse(int.MinValue, "...", 0, 0, null, null, null, null, null, null, null)
						: new ProductResponse(product.Id, _product.Name, _product.Count, _product.Price, _product.Unit, _product.Image,
					_product.Description, _product.Manufacturer, _product.Provider, _product.Discount,
					Products.GetCategories(product.Id).Select(category => new CategoryResponse(category.Id, category.Name)).ToArray());
				}).ToArray())).ToArray(), statusCode: 201);
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapPost("/api/orders", [Authorize] (OrderCreateRequest request, ClaimsPrincipal user) => {
			if (!IsAdmin(user) && GetUserId(user) != request.UserId)
				return Results.Forbid();

			try {
				var order = Orders.New(request.UserId, request.Products.Select(product => (product.Id, product.Count)).ToArray());

				return Results.Json(new OrderResponse(order.Id, order.UserId,
					request.Products.Select((product) => {
						var _product = Products.Get(product.Id);

						return _product is null
							? new ProductResponse(int.MinValue, "...", 0, 0, null, null, null, null, null, null, null)
							: new ProductResponse(product.Id, _product.Name, _product.Count, _product.Price, _product.Unit, _product.Image,
						_product.Description, _product.Manufacturer, _product.Provider, _product.Discount,
						Products.GetCategories(product.Id).Select(category => new CategoryResponse(category.Id, category.Name)).ToArray());
					}).ToArray()), statusCode: 201);
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapPut("/api/orders/{id:int}", [Authorize(Roles = Role.Admin)] (int id, OrderUpdateRequest request) => {
			try {
				Orders.Update(id, request.UserId, request.Products);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.InternalServerError(new ErrorResponse(e.Message));
			} catch (ValidationError e) {
				return Results.UnprocessableEntity(new ErrorResponse(e.Message));
			}
		});

		app.MapDelete("/api/orders/{id:int}", [Authorize(Roles = Role.Admin)] (int id) => {
			try {
				Orders.Delete(id);
				return Results.Ok();
			} catch (DatabaseException e) {
				return Results.NotFound(new ErrorResponse(e.Message));
			}
		});
	}
}