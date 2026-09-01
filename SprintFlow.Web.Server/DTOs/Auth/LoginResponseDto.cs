namespace SprintFlow.Web.Server.DTOs.Auth;

// IsAdmin is decided on the server by comparing UserId with Auth:AdminUserId. The client applies
// it; it never re-derives it from the token.
public record LoginResponseDto(string Token, string FullName, bool IsAdmin);
