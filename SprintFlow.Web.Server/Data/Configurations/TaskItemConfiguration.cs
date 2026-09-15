using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.TaskId);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasDefaultValue(TaskPriority.Normal)
            .HasSentinel((TaskPriority)(-1));

        builder.Property(t => t.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(t => t.Board)
            .WithMany(b => b.Tasks)
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => new { t.TaskColumnId, t.BoardId })
            .HasPrincipalKey(c => new { c.WorkflowColumnId, c.BoardId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Sprint)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => new { t.SprintId, t.BoardId })
            .HasPrincipalKey(s => new { s.SprintId, s.BoardId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TaskColumnId, t.Position });

        builder.HasIndex(t => t.BoardId);
    }
}
