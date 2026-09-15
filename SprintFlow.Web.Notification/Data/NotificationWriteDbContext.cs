using Microsoft.EntityFrameworkCore;

namespace SprintFlow.Web.Notification.Data;

public class NotificationWriteDbContext : DbContext
{
    public NotificationWriteDbContext(DbContextOptions<NotificationWriteDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationRow> Notifications => Set<NotificationRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var notification = modelBuilder.Entity<NotificationRow>();

        notification.ToTable("Notifications");
        notification.HasKey(n => n.NotificationId);

        notification.Property(n => n.Type).IsRequired();

        notification.Property(n => n.RecipientUserId).IsRequired();
        notification.Property(n => n.ActorUserId).IsRequired();
        notification.Property(n => n.TaskId).IsRequired();
    }
}
