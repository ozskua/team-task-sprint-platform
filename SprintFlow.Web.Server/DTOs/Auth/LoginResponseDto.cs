namespace SprintFlow.Web.Server.DTOs.Auth;

// IsAdmin is decided on the server by comparing UserId with Auth:AdminUserId. The client applies
// it; it never re-derives it from the token.
//
// UserId is here so the client can recognise its own rows - which comment it may offer to delete,
// for one. It is not a permission: the server checks the author again on every write.
public record LoginResponseDto(int UserId, string Token, string FullName, bool IsAdmin);
