using RecruitmentAts.Infrastructure.Security;

namespace RecruitmentAts.Infrastructure.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verifies_the_right_password_only()
    {
        var hash = _hasher.Hash("Mật khẩu đúng 123");

        Assert.True(_hasher.Verify(hash, "Mật khẩu đúng 123"));
        Assert.False(_hasher.Verify(hash, "Mật khẩu sai 123"));
    }

    [Fact]
    public void Hash_is_salted_and_records_its_parameters()
    {
        var first = _hasher.Hash("same password");
        var second = _hasher.Hash("same password");

        Assert.NotEqual(first, second);
        Assert.StartsWith("pbkdf2-sha256$600000$", first, StringComparison.Ordinal);
        Assert.DoesNotContain("same password", first, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("md5$1$abc$def")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$not base64$AAAA")]
    public void Malformed_hash_never_verifies(string hash) => Assert.False(_hasher.Verify(hash, "password"));
}
