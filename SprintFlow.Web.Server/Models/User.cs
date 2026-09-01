namespace SprintFlow.Web.Server.Models;

public class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public Role Role { get; set; } = null!;

    // Composed at read time, never a column - see UserConfiguration.
    public string FullName => $"{FirstName} {LastName}".Trim();
}
