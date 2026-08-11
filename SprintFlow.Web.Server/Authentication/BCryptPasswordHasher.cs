namespace SprintFlow.Web.Server.Authentication;

public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Matches the work factor the seeded hashes were produced with, so verification of a real user
    /// and of the dummy hash below cost the same.
    /// </summary>
    private const int WorkFactor = 12;

    /// <summary>
    /// A hash of a random value generated once at startup. Nothing can ever match it; its only job
    /// is to consume the same time a real verification would.
    /// </summary>
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    public void VerifyAgainstDummy(string password)
        => BCrypt.Net.BCrypt.Verify(password, DummyHash);
}
