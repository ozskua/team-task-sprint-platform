using System.Security.Claims;

namespace SprintFlow.Web.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetAuthenticatedUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(claim, out var userId))
        {
            throw new InvalidOperationException(
                "The authenticated token carries no usable NameIdentifier claim.");
        }

        return userId;
    }
}
