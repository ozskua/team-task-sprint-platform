using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SprintFlow.Web.Server.Models;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Authentication;

// Only issues tokens. Validating incoming tokens is configured separately in
// ServiceCollectionExtensions; both read the same Jwt settings.
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions options;
    private readonly SigningCredentials signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.options.Key));
        signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(User user)
    {
        var userId = user.UserId.ToString();

        // JWTs are signed, not encrypted; never put secrets in claims.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId), // What the server reads to answer "who is calling?".
            new(ClaimTypes.Role, user.Role.RoleName), // Role name, not id: [Authorize(Roles = "Admin")] compares strings.
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes),
            SigningCredentials = signingCredentials
        };

        // JsonWebTokenHandler writes claims as given; the older handler rewrites 'sub'.
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
