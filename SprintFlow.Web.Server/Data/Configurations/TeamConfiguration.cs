using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.TeamId);

        builder.Property(t => t.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(t => t.Board)
            .WithOne(b => b.Team)
            .HasPrincipalKey<Board>(b => new { b.BoardId, b.CreatedByUserId })
            .HasForeignKey<Team>(t => new { t.BoardId, t.CreatedByUserId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.BoardId)
            .IsUnique();

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
