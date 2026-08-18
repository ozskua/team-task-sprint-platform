using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;

namespace SprintFlow.Web.Server.Services;

public class UserService : IUserService
{
    private readonly IUserRepository userRepository;

    public UserService(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        // Thin on purpose: reading a list has no business rule to apply yet. The layer exists so
        // the controller never reaches the repository, and so the rules have somewhere to go.
        return users.Select(user => user.ToListItemDto()).ToList();
    }
}
