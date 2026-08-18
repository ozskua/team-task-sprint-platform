namespace SprintFlow.Web.Server.DTOs.Users;

/// <summary>
/// One row of the Admin users list. The password hash is absent by construction — the entity is
/// never serialized, only this record is.
/// </summary>
/// <param name="UserId">Identifies the row; not displayed.</param>
/// <param name="FullName">Composed from the first and last name at read time.</param>
/// <param name="Email">The address the account signs in with.</param>
/// <param name="RoleName">The global role: <c>Admin</c> or <c>Member</c>. Says nothing about
/// Areas, Boards, Teams or Tasks.</param>
/// <param name="IsActive">Whether the account can sign in.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
public record UserListItemDto(
    int UserId,
    string FullName,
    string Email,
    string RoleName,
    bool IsActive,
    DateTime CreatedDate);
