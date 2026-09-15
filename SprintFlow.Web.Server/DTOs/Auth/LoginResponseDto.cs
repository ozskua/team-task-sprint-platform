namespace SprintFlow.Web.Server.DTOs.Auth;

public record LoginResponseDto(int UserId, string Token, string FullName, bool IsAdmin);
