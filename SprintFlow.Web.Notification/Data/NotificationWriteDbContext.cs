using Microsoft.EntityFrameworkCore;

namespace SprintFlow.Web.Notification.Data;

/// <summary>
/// The one table this service writes to.
/// </summary>
/// <remarks>
/// <para>
/// <b>This context owns no schema and generates no migration.</b> The API service holds the full
/// model and every migration; this one only describes enough of the <c>Notifications</c> table to
/// insert into it. Running <c>dotnet ef migrations add</c> against this project would be a mistake:
/// it would produce a migration that believes the database has exactly one table.
/// </para>
/// <para>
/// Only the columns this service sets are mapped. <c>IsRead</c> and <c>CreatedDate</c> are left to
/// the database defaults and are therefore not described here at all — which is also why an INSERT
/// from this service and one from the API service produce identical rows.
/// </para>
/// </remarks>
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

        // The enum is stored as its int value, matching the column the API service created.
        notification.Property(n => n.Type).IsRequired();

        notification.Property(n => n.RecipientUserId).IsRequired();
        notification.Property(n => n.ActorUserId).IsRequired();
        notification.Property(n => n.TaskId).IsRequired();
    }
}
