using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Fixed timestamp for the seeded rows. <c>HasData</c> has to be deterministic — a value such as
    /// <c>DateTime.UtcNow</c> would differ on every run and rewrite the model snapshot each time.
    /// </summary>
    private static readonly DateTime SeedCreatedDate = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private const int AdminRoleId = 1;
    private const int MemberRoleId = 2;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Pinned away from the server's Turkish collation, where 'i' and 'I' are different letters.
        // Under Turkish rules "AYSE.DEMIR@..." would not match "ayse.demir@..." on login, and the
        // unique index would allow both as separate accounts.
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256)
            .UseCollation("Latin1_General_100_CI_AS");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Two accounts must never share an address: the address is what login resolves a user by.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.RoleId)
            .HasDatabaseName("IX_Users_RoleId");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        // Restrict, not Cascade: deleting a role must never delete the people who hold it.
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composed from FirstName and LastName, so there is nothing to store.
        builder.Ignore(u => u.FullName);

        // Development accounts. The identities are invented; no real person's data is used here.
        // Passwords are BCrypt hashes at work factor 12 ($2a$12$...) — no plaintext is stored, and
        // the hashes are constants for the same determinism reason as SeedCreatedDate above.
        builder.HasData(
            new User
            {
                UserId = 1,
                FirstName = "Ayşe",
                LastName = "Demir",
                Email = "ayse.demir@sprintflow.local",
                PasswordHash = "$2a$12$PAqHNBJ/AqN4D9UJsktCKerPo2bqhqnRxLj6VOmLHxLPbMiIIxn/a",
                RoleId = AdminRoleId,
                IsActive = true,
                CreatedDate = SeedCreatedDate
            },
            new User
            {
                UserId = 2,
                FirstName = "Mert",
                LastName = "Kaya",
                Email = "mert.kaya@sprintflow.local",
                PasswordHash = "$2a$12$IEsozBUG66RHg4u5uwF6XuFZp4ZbcUQtvBP16wPZzf4X4h6TOljR2",
                RoleId = MemberRoleId,
                IsActive = true,
                CreatedDate = SeedCreatedDate
            },
            new User
            {
                UserId = 3,
                FirstName = "Elif",
                LastName = "Yıldız",
                Email = "elif.yildiz@sprintflow.local",
                PasswordHash = "$2a$12$LZwgxyQpuU2NOWSjm.szSOXsiJgwT26b4LYRzI3xR/mAeGrU5pgQO",
                RoleId = MemberRoleId,
                IsActive = true,
                CreatedDate = SeedCreatedDate
            },
            new User
            {
                UserId = 4,
                FirstName = "Burak",
                LastName = "Şahin",
                Email = "burak.sahin@sprintflow.local",
                PasswordHash = "$2a$12$yIOuFoEm4ZkFY1sbn6U4c.BoqbsqCLxg08mTwfiqCYz3sReIwL2tC",
                RoleId = MemberRoleId,
                IsActive = true,
                CreatedDate = SeedCreatedDate
            },
            new User
            {
                UserId = 5,
                FirstName = "Zeynep",
                LastName = "Arslan",
                Email = "zeynep.arslan@sprintflow.local",
                PasswordHash = "$2a$12$d0DKlM7jjdirdf1Mk5aGWO5vETEwZqP29h3/S8s4TJtr29Z6EFC3G",
                RoleId = MemberRoleId,
                IsActive = true,
                CreatedDate = SeedCreatedDate
            });
    }
}
