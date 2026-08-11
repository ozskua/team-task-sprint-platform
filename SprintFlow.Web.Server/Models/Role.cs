namespace SprintFlow.Web.Server.Models;

/// <summary>
/// The set of roles a user can hold. Modelled as its own table rather than a string column on
/// <see cref="User"/> so the value set is closed and enforced by a foreign key.
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
