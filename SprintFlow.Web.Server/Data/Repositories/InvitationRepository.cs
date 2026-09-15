using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class InvitationRepository
{
    private readonly SprintFlowDbContext context;

    public InvitationRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public void Add(Invitation invitation) => context.Invitations.Add(invitation);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public Task<Invitation?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken cancellationToken)
        => context.Invitations
            .AsNoTracking()
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public Task<Invitation?> GetForUpdateAsync(byte[] tokenHash, CancellationToken cancellationToken)
        => context.Invitations
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public async Task<TResult> RunRedemptionAsync<TResult>(
        byte[] tokenHash,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT TOP 1 InvitationId FROM Invitations WITH (UPDLOCK, HOLDLOCK) WHERE TokenHash = {tokenHash}",
                cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
