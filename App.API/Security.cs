namespace App.API;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using App.Database.Models;

public static class Auth {
	private static SymmetricSecurityKey securityKey = null!;

	public static void Initialize(string secret) {
		if (secret.Length < 32)
			throw new InvalidOperationException("Длина секрета JWT должны быть хотя бы 32 байта.");

		securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
	}

	public static string GenerateToken(User user) {
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.Login),
			new Claim(ClaimTypes.Role, user.Role)
		};

		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: null,
			audience: null,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(24),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public static ClaimsPrincipal? ValidateToken(string token) {
		try {
			var tokenHandler = new JwtSecurityTokenHandler();
			var validationParameters = new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = securityKey,
				ValidateIssuer = false,
				ValidateAudience = false,
				ClockSkew = TimeSpan.Zero
			};

			var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
			return principal;
		} catch {
			return null;
		}
	}

	public static int? GetUserIdFromToken(string token) {
		var principal = ValidateToken(token);
		var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);

		return userIdClaim is not null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
	}
}