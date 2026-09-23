using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuraRental.Domain.Entities;
using AuraRental.Service.Interface.Service;
using Microsoft.IdentityModel.Tokens;

namespace AuraRental.WebAPI.Services;

public sealed class JwtAccessTokenService(IConfiguration configuration) : IAccessTokenService
{
    public AccessToken Create(User user)
    {
        var signingKey = configuration["Authentication:SigningKey"]
            ?? throw new InvalidOperationException("Authentication:SigningKey is required.");
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes.");
        }

        var issuer = configuration["Authentication:Issuer"]
            ?? throw new InvalidOperationException("Authentication:Issuer is required.");
        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is required.");
        var lifetimeMinutes = configuration.GetValue("Authentication:AccessTokenMinutes", 480);
        if (lifetimeMinutes is < 5 or > 1_440)
        {
            throw new InvalidOperationException("Authentication:AccessTokenMinutes must be between 5 and 1440.");
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(lifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}
