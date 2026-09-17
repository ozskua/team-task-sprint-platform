using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class UserRepository
{
    private readonly SprintFlowDbContext context;

    public UserRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public Task<User?> GetByEmailAsync(string canonicalEmail, CancellationToken cancellationToken)
        => context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == canonicalEmail, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => context.Users.CountAsync(cancellationToken);

    public Task<bool> ExistsAsync(int userId, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.UserId == userId && u.IsActive, cancellationToken);

    public Task<bool> EmailExistsAsync(string canonicalEmail, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.Email == canonicalEmail, cancellationToken);

    public Task<User?> GetForUpdateAsync(int userId, CancellationToken cancellationToken)
        => context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<string?> GetProfilePhotoKeyAsync(int userId, CancellationToken cancellationToken)
        => context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.ProfilePhotoKey)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(User user) => context.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
