namespace SprintFlow.Web.Server.DTOs.Users;

public record UserListItemDto(
    int UserId,
    string FullName,
    string Email,
    string RoleName,
    bool IsActive,
    DateTime CreatedDate);
