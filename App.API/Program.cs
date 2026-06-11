namespace App.API;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using App.Database;
using App.Database.Models;

internal static class Program {
	private static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);

		var secret = builder.Configuration["Auth:Secret"]
			?? throw new InvalidOperationException("Секрет JWT не предоставлен.");

		var key = Encoding.ASCII.GetBytes(secret);

		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
			options.TokenValidationParameters = new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false,
				ClockSkew = TimeSpan.Zero
			};

			options.Events = new JwtBearerEvents {
				OnMessageReceived = context => {
					var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
					context.Token = token;
					return Task.CompletedTask;
				}
			};
		});

		builder.Services.AddAuthorization();
		builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

		var app = builder.Build();

		app.UseCors("AllowAll");
		app.UseAuthentication();
		app.UseAuthorization();

		Database.Connect(app.Configuration.GetConnectionString("Default") ?? "");
		Auth.Initialize(secret);

		API.Initialize(app);
		app.Run();
	}
}