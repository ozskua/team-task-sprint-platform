namespace SprintFlow.Web.Server.Authentication;

public interface IPasswordHasher
{
    const int MaxPasswordBytes = 72;

    string Hash(string password);

    bool Verify(string password, string passwordHash);

    void VerifyAgainstDummy(string password);
}
