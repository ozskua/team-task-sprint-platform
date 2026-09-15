using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");

        builder.HasKey(c => c.TaskCommentId);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TaskId, c.CreatedDate });
    }
}
