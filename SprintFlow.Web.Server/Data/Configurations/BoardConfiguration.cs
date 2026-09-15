using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");

        builder.HasKey(b => b.BoardId);

        builder.HasAlternateKey(b => new { b.BoardId, b.CreatedByUserId });

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(b => b.Area)
            .WithMany(a => a.Boards)
            .HasForeignKey(b => b.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
