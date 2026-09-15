namespace SprintFlow.Web.Server.Authentication;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    public void VerifyAgainstDummy(string password)
        => BCrypt.Net.BCrypt.Verify(password, DummyHash);
}
