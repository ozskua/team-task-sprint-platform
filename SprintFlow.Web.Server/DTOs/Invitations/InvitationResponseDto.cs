namespace SprintFlow.Web.Server.DTOs.Invitations;

// The only time the token is readable: only its SHA-256 digest is stored. No registration URL -
// the browser composes that from its own origin.
public record InvitationResponseDto(string Token, DateTime ExpiresAt);
