namespace SprintFlow.Web.Server.Common;

public class AuthenticationFailedException : Exception
{
    public const string GenericMessage = "Invalid e-mail or password.";

    public AuthenticationFailedException()
        : base(GenericMessage)
    {
    }
}
