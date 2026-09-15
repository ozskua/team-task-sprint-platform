using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.Type)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Task)
            .WithMany()
            .HasForeignKey(n => n.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedDate });
    }
}
