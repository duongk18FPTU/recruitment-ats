namespace RecruitmentAts.Application.Candidates;

/// <summary>The keys that identify a person (BR-06): the same address or number written differently must match.</summary>
public static class ContactNormalizer
{
    /// <summary>Trimmed and lower-cased; null when empty.</summary>
    public static string? Email(string? email)
    {
        var value = email?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Digits only, with a Vietnamese country code written as the national leading zero
    /// (<c>+84 912 345 678</c> and <c>0912.345.678</c> are the same number); null when there are no digits.
    /// </summary>
    public static string? Phone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string([.. phone.Where(char.IsAsciiDigit)]);
        if (digits.Length == 0)
        {
            return null;
        }

        // 84 + a 9-digit national number; "+84 (0)912…" keeps its zero.
        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length >= 11)
        {
            var national = digits[2..];
            digits = national.StartsWith('0') ? national : "0" + national;
        }

        return digits.Length <= 15 ? digits : digits[..15];
    }
}
