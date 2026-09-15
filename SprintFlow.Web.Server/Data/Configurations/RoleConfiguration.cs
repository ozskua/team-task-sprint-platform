using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.RoleId);

        builder.Property(r => r.RoleName)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.RoleName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_RoleName");

        builder.HasData(
            new Role { RoleId = 1, RoleName = RoleNames.Admin },
            new Role { RoleId = 2, RoleName = RoleNames.Member });
    }
}
