namespace SprintFlow.Web.Server.Common;

/// <summary>
/// Thrown for every failed sign-in, whatever the actual cause — unknown e-mail, wrong password or a
/// disabled account. One exception type with one message is deliberate: the caller must not be able
/// to tell which of the three happened.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public const string GenericMessage = "Invalid e-mail or password.";

    public AuthenticationFailedException()
        : base(GenericMessage)
    {
    }
}
