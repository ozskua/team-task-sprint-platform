using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SprintFlowDbContext context;

    public UserRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public Task<User?> GetByEmailAsync(string canonicalEmail, CancellationToken cancellationToken)
        => context.Users
            .AsNoTracking() // Login only reads the user; no change tracking is needed.
            .Include(u => u.Role) // The role name goes into the token, so it must be loaded here.
            .FirstOrDefaultAsync(u => u.Email == canonicalEmail, cancellationToken);
}
