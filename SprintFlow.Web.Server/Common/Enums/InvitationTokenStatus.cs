namespace SprintFlow.Web.Server.Common.Enums;

// The causes are reported separately, unlike a failed sign-in. That rule exists because e-mail
// addresses are guessable; a 256-bit token is not, so naming the reason tells an attacker nothing.
public enum InvitationTokenStatus
{
    Valid,

    NotFound,

    AlreadyUsed,

    Expired,

    InviterInactive
}
