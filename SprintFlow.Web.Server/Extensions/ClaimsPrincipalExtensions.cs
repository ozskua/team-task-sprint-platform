using System.Security.Claims;

namespace SprintFlow.Web.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The caller, taken from the validated token. Request bodies have no say in who the caller is —
    /// a client that sends its own id is simply not modelled by any request DTO.
    /// </summary>
    /// <remarks>
    /// Lives here rather than in each controller: four of them now need it, and one copy is the only
    /// way to be sure they all read the same claim and fail the same way.
    /// </remarks>
    public static int GetAuthenticatedUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // Every token this server issues carries the claim, so its absence means the server is
        // misconfigured — not that the caller did anything wrong. A 500 states that honestly.
        if (!int.TryParse(claim, out var userId))
        {
            throw new InvalidOperationException(
                "The authenticated token carries no usable NameIdentifier claim.");
        }

        return userId;
    }
}
