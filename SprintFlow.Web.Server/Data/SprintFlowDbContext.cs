using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data;

public class SprintFlowDbContext : DbContext
{
    public SprintFlowDbContext(DbContextOptions<SprintFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<WorkflowColumn> WorkflowColumns => Set<WorkflowColumn>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<TaskParticipant> TaskParticipants => Set<TaskParticipant>();

    public DbSet<Sprint> Sprints => Set<Sprint>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SprintFlowDbContext).Assembly);
    }
}
