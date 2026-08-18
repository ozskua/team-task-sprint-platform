using SprintFlow.Web.Server.DTOs.Users;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IUserService
{
    /// <summary>
    /// Every user in the company, active and inactive, ordered by name. There is no paging: the
    /// list is small and bounded by the number of employees.
    /// </summary>
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken);
}
