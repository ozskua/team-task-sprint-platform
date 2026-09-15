using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class TaskParticipantConfiguration : IEntityTypeConfiguration<TaskParticipant>
{
    public void Configure(EntityTypeBuilder<TaskParticipant> builder)
    {
        builder.ToTable("TaskParticipants");

        builder.HasKey(p => new { p.TaskId, p.UserId });

        builder.Property(p => p.AddedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(p => p.Task)
            .WithMany(t => t.Participants)
            .HasForeignKey(p => p.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.UserId);

        builder.HasIndex(p => p.TaskId)
            .IsUnique();
    }
}
