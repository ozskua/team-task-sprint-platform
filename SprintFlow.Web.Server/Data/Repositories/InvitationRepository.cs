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
            .AsNoTracking() // Only read to decide whether the link is usable; nothing is changed.
            // The inviter's account has to still be usable for the invitation to be, so the row is
            // needed rather than just the foreign key.
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public Task<Invitation?> GetForUpdateAsync(byte[] tokenHash, CancellationToken cancellationToken)
        => context.Invitations
            // Tracked, unlike the read above: this row is about to be marked used in the same
            // transaction that creates the account.
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public async Task<TResult> RunRedemptionAsync<TResult>(
        byte[] tokenHash,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        // The connection is configured to retry transient failures, and a retrying strategy refuses
        // to manage a transaction it did not open itself — so the whole unit is handed to it rather
        // than a transaction being started around it.
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs everything below. Starting from a clean tracker means the second
            // attempt reads current rows instead of reusing the half-changed entities the first
            // attempt left behind.
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            // The gate. UPDLOCK is incompatible with another UPDLOCK, so a second request holding
            // the same token blocks here; HOLDLOCK keeps it until the transaction ends, so it blocks
            // until the first request's UsedAt is committed rather than until this statement
            // finishes. That is what makes redemption single-use under concurrency: the loser reads
            // the row only after the winner has marked it, and sees it already spent.
            //
            // HOLDLOCK also locks the range when the digest matches nothing, so two callers
            // presenting the same unknown token cannot both proceed.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT TOP 1 InvitationId FROM Invitations WITH (UPDLOCK, HOLDLOCK) WHERE TokenHash = {tokenHash}",
                cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
