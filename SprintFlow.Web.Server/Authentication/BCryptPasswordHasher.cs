namespace SprintFlow.Web.Server.Authentication;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Must match the factor the seeded hashes were created with.

    // Hash of a random value; nothing can match it. Only used to spend the same time a real
    // verification would.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    public void VerifyAgainstDummy(string password)
        => BCrypt.Net.BCrypt.Verify(password, DummyHash); // Result is ignored; the cost is the point.
}
