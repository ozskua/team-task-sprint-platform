namespace SprintFlow.Web.Server.Authentication;

public interface IPasswordHasher
{
    /// <summary>Checks a submitted password against a stored hash.</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Runs the same work against a throwaway hash. Called when no user matches the e-mail, so that
    /// an unknown address costs roughly as long as a wrong password and cannot be identified by
    /// response time.
    /// </summary>
    void VerifyAgainstDummy(string password);
}
