using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RecruitmentAts.Infrastructure.Persistence;

/// <summary>
/// Stores an enum as the UPPER_SNAKE_CASE text used by the database and the API
/// (<c>HrManager</c> ↔ <c>HR_MANAGER</c>), with the same naming policy as System.Text.Json.
/// </summary>
internal sealed class UpperSnakeEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => ToDatabase(value),
    text => FromDatabase(text))
    where TEnum : struct, Enum
{
    public static string ToDatabase(TEnum value) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString());

    public static TEnum FromDatabase(string text) =>
        Enum.Parse<TEnum>(text.Replace("_", "", StringComparison.Ordinal), ignoreCase: true);
}
