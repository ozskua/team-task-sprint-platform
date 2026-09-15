using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class WorkflowColumnConfiguration : IEntityTypeConfiguration<WorkflowColumn>
{
    public void Configure(EntityTypeBuilder<WorkflowColumn> builder)
    {
        builder.ToTable("WorkflowColumns");

        builder.HasKey(c => c.WorkflowColumnId);

        builder.HasAlternateKey(c => new { c.WorkflowColumnId, c.BoardId });

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(c => c.Board)
            .WithMany(b => b.WorkflowColumns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.BoardId, c.Position });

        builder.HasIndex(c => new { c.BoardId, c.SystemKind })
            .IsUnique()
            .HasFilter("[SystemKind] IS NOT NULL");
    }
}
