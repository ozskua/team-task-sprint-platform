using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    private const int TokenHashLength = 32;

    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(i => i.InvitationId);

        builder.Property(i => i.TokenHash)
            .IsRequired()
            .HasColumnType($"varbinary({TokenHashLength})");

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.Property(i => i.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(i => i.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_Invitations_TokenHash");

        builder.HasIndex(i => i.InvitedByUserId)
            .HasDatabaseName("IX_Invitations_InvitedByUserId");

        builder.HasIndex(i => i.RegisteredUserId)
            .IsUnique()
            .HasFilter("[RegisteredUserId] IS NOT NULL")
            .HasDatabaseName("IX_Invitations_RegisteredUserId");

        builder.HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RegisteredUser)
            .WithMany()
            .HasForeignKey(i => i.RegisteredUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
