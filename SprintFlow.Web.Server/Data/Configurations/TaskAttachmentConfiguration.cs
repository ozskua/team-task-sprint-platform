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

        // A GUID plus an extension; never anything the uploader chose.
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

        // One row per file on disk, so a stored name can never address two attachments.
        builder.HasIndex(a => a.StoredName)
            .IsUnique()
            .HasDatabaseName("IX_TaskAttachments_StoredName");

        // Cascade: the metadata has no meaning without its task. The file on disk is removed by the
        // service, which is the only thing that can - a cascade cannot delete from the filesystem.
        builder.HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, and deliberately not Cascade: Tasks already cascades to here, so a second
        // cascading path Tasks -> TaskComments -> TaskAttachments would be rejected by SQL Server
        // as multiple cascade paths (error 1785). Deleting a comment therefore removes its
        // attachments in the service, which has to delete their files from disk anyway.
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
