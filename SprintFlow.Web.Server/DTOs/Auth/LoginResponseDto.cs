namespace SprintFlow.Web.Server.DTOs.Auth;

/// <summary>
/// What a successful sign-in returns. Deliberately narrow — no password hash, no role id, nothing
/// the client has no use for.
/// </summary>
/// <param name="Token">Signed JWT.</param>
/// <param name="FullName">Display name for the shell header.</param>
/// <param name="IsAdmin">
/// The dashboard decision, already made on the server by comparing the authenticated
/// <c>UserId</c> with the configured <c>Auth:AdminUserId</c>. The client applies it; it does not
/// re-derive it.
/// </param>
public record LoginResponseDto(string Token, string FullName, bool IsAdmin);
