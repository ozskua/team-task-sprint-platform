using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data;

/// <summary>
/// EF Core context for SprintFlow. Only the two tables authentication needs exist so far; the
/// remaining tables arrive with their own features.
/// </summary>
public class SprintFlowDbContext : DbContext
{
    public SprintFlowDbContext(DbContextOptions<SprintFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in this assembly, so adding an entity means
        // adding one configuration file — this method never changes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SprintFlowDbContext).Assembly);
    }
}
