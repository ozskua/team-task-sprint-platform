using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

/// <summary>
/// Hand-written entity to DTO mapping. Written out rather than generated so that what leaves the
/// server is visible in one place — most importantly, that <c>PasswordHash</c> is not copied.
/// </summary>
public static class UserMappings
{
    /// <summary>Requires <c>Include(u =&gt; u.Role)</c>; the role name is read from the navigation.</summary>
    public static UserListItemDto ToListItemDto(this User user)
        => new(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.IsActive,
            user.CreatedDate);
}
