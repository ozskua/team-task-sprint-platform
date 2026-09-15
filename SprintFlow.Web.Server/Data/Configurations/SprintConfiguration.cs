using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("Sprints", table =>
            table.HasCheckConstraint("CK_Sprints_EndAfterStart", "[EndDate] > [StartDate]"));

        builder.HasKey(s => s.SprintId);

        builder.HasAlternateKey(s => new { s.SprintId, s.BoardId });

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(s => s.EndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(s => s.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(s => s.Board)
            .WithMany(b => b.Sprints)
            .HasForeignKey(s => s.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.BoardId, s.StartDate });
    }
}
