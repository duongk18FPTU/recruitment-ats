using System.Globalization;
using System.Security.Cryptography;
using RecruitmentAts.Application.Abstractions.Security;

namespace RecruitmentAts.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 (OWASP: 600,000 iterations), stored as
/// <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;</c> with Base64 salt and hash.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha256";
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return string.Join('$', Scheme, Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string hash, string password)
    {
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(password);

        var parts = hash.Split('$');
        if (parts.Length != 4
            || parts[0] != Scheme
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
