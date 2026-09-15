using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("TaskAttachments");

        builder.HasKey(a => a.TaskAttachmentId);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.StoredName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(a => a.SizeBytes)
            .IsRequired();

        builder.Property(a => a.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(a => a.StoredName)
            .IsUnique()
            .HasDatabaseName("IX_TaskAttachments_StoredName");

        builder.HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Comment)
            .WithMany(c => c.Attachments)
            .HasForeignKey(a => a.TaskCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TaskId);

        builder.HasIndex(a => a.TaskCommentId);
    }
}
