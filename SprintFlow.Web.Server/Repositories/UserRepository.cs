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

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Users
            .AsNoTracking() // A list is read and discarded; change tracking would only cost memory.
            .Include(u => u.Role) // The role name is shown in the list, so it must be loaded here.
            // Sorted in SQL under the database's Turkish collation, which orders Ç, Ğ, İ, Ö, Ş, Ü
            // where a Turkish reader expects them. Only Email is pinned to a different collation.
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => context.Users.CountAsync(cancellationToken);

    public Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.UserId == userId && u.IsActive, cancellationToken);
}
