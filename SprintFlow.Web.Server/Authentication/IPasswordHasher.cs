namespace SprintFlow.Web.Server.Authentication;

public interface IPasswordHasher
{
    /// <summary>
    /// The maximum number of UTF-8 bytes bcrypt actually reads from a password.
    /// </summary>
    /// <remarks>
    /// Bytes, not characters — a Turkish password of 40 characters is 80 bytes and would be cut.
    /// Measured against the installed BCrypt.Net-Next 4.2.0: hashing a longer password does not
    /// throw, it silently ignores everything past this point, so two passwords sharing their first
    /// 72 bytes both verify against the same hash. Callers validate against this before hashing so
    /// the caller is told, rather than quietly getting a weaker password than they typed.
    /// </remarks>
    const int MaxPasswordBytes = 72;

    /// <summary>
    /// Hashes a password for storage. The plaintext is never kept, and the same work factor the
    /// seeded accounts were created with is used, so old and new accounts verify identically.
    /// </summary>
    string Hash(string password);

    /// <summary>Checks a submitted password against a stored hash.</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Runs the same work against a throwaway hash. Called when no user matches the e-mail, so that
    /// an unknown address costs roughly as long as a wrong password and cannot be identified by
    /// response time.
    /// </summary>
    void VerifyAgainstDummy(string password);
}
