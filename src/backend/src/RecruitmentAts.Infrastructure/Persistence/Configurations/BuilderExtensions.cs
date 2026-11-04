using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RecruitmentAts.Infrastructure.Persistence.Configurations;

internal static class BuilderExtensions
{
    /// <summary>
    /// Column default from schema.sql, kept for manual inserts. The application always sends
    /// the value it set, so a CLR default such as <c>false</c> is never replaced by the database.
    /// </summary>
    public static PropertyBuilder<T> HasDbDefault<T>(this PropertyBuilder<T> property, string sql) =>
        property.HasDefaultValueSql(sql).ValueGeneratedNever();

    /// <summary>Adds <c>CHECK (column IN (...))</c> with the stored values of <typeparamref name="TEnum"/>.</summary>
    public static TableBuilder HasValues<TEnum>(this TableBuilder table, string column)
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>().Select(value => $"'{UpperSnakeEnumConverter<TEnum>.ToDatabase(value)}'");
        table.HasCheckConstraint($"ck_{table.Name}_{column}", $"{column} IN ({string.Join(", ", values)})");
        return table;
    }
}
