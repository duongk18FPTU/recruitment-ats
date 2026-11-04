using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RecruitmentAts.Infrastructure.Persistence;

/// <summary>
/// EF Core model of docs/database/schema.sql. Tables are added per milestone
/// (docs/12-delivery-plan.md); SchemaMatchesReferenceTests compares the migrated
/// database with schema.sql.
/// </summary>
public sealed class AtsDbContext(DbContextOptions<AtsDbContext> options) : DbContext(options)
{
    // schema.sql declares every index explicitly; EF's automatic foreign-key indexes are not wanted.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("position_code_seq");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            StoreEnumsAsUpperSnakeText(entityType);
        }
    }

    // Enumerations are text columns with CHECK constraints (schema.sql conventions).
    private static void StoreEnumsAsUpperSnakeText(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (enumType.IsEnum)
            {
                var converterType = typeof(UpperSnakeEnumConverter<>).MakeGenericType(enumType);
                property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
            }
        }
    }
}
