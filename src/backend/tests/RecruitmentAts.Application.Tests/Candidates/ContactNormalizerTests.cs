using RecruitmentAts.Application.Candidates;

namespace RecruitmentAts.Application.Tests.Candidates;

/// <summary>BR-06: the same email or number written differently must find the same candidate.</summary>
public class ContactNormalizerTests
{
    [Theory]
    [InlineData("Lan.Tran@Example.TEST", "lan.tran@example.test")]
    [InlineData("  lan@example.test  ", "lan@example.test")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Email_is_trimmed_and_lower_cased(string? input, string? expected) =>
        Assert.Equal(expected, ContactNormalizer.Email(input));

    [Theory]
    [InlineData("0912 345 678", "0912345678")]
    [InlineData("0912.345.678", "0912345678")]
    [InlineData("(091) 234-5678", "0912345678")]
    [InlineData("+84 912 345 678", "0912345678")]
    [InlineData("+84912345678", "0912345678")]
    [InlineData("84912345678", "0912345678")]
    [InlineData("+84 (0)912 345 678", "0912345678")]
    [InlineData("0243 123 4567", "02431234567")]
    [InlineData("abc", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Phone_keeps_digits_and_writes_the_country_code_as_the_national_zero(string? input, string? expected) =>
        Assert.Equal(expected, ContactNormalizer.Phone(input));

    [Fact]
    public void Phone_is_cut_to_the_15_digits_the_column_holds() =>
        Assert.Equal(15, ContactNormalizer.Phone("1234567890123456789")!.Length);
}
