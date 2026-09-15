using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace PersonalDiary.Services;

public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(IdentityUser user);
}

public sealed class JwtTokenService(
    IConfiguration configuration,
    UserManager<IdentityUser> userManager) : IJwtTokenService
{
    public async Task<string> CreateTokenAsync(IdentityUser user)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = jwt["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var expirationMinutes = jwt.GetValue("ExpirationMinutes", 60);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(await userManager.GetClaimsAsync(user));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
