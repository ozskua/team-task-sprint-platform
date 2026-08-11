namespace SprintFlow.Web.Server.Models;

/// <summary>
/// An application user. The role is held as a foreign key to <see cref="Models.Role"/>, never as a
/// string on this row.
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hash of the password. The plaintext is never stored and never returned to a client.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Loaded with <c>Include(u =&gt; u.Role)</c> when the role name is needed — for example when a
    /// token is built and the role claim has to carry the name rather than the numeric id.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>Composed at read time, not stored — see <c>UserConfiguration</c>.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
