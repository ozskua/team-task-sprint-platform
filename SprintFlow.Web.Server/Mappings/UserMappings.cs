using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class UserMappings
{
    public static UserListItemDto ToListItemDto(this User user)
        => new(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.IsActive,
            user.CreatedDate);
}
