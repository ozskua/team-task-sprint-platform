using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SprintFlow.Web.Server.Models;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Authentication;

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

        var claims = new List<Claim>
        {
            // Standard JWT subject — "who is this token about".
            new(JwtRegisteredClaimNames.Sub, userId),

            // What User.FindFirstValue(ClaimTypes.NameIdentifier) reads server-side once endpoints
            // need to answer "who is making this request".
            new(ClaimTypes.NameIdentifier, userId),

            // The role *name*, not the numeric RoleId: [Authorize(Roles = "Admin")] compares strings.
            new(ClaimTypes.Role, user.Role.RoleName),

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

        // JsonWebTokenHandler writes claims exactly as given. The older JwtSecurityTokenHandler
        // rewrites some claim names on the way out, which would corrupt `sub`.
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
