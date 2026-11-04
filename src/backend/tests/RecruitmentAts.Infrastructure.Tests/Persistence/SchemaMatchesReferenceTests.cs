using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RecruitmentAts.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RecruitmentAts.Infrastructure.Tests.Persistence;

/// <summary>
/// The database produced by the EF Core migrations must match docs/database/schema.sql
/// for every table the model already has (tables arrive per milestone).
/// </summary>
public sealed partial class SchemaMatchesReferenceTests(SchemaDatabases databases) : IClassFixture<SchemaDatabases>
{
    private readonly string _migrated = databases.Migrated;
    private readonly string _reference = databases.Reference;
    private readonly string[] _tables = databases.Tables;

    [Fact]
    public async Task Every_model_table_exists_in_schema_sql()
    {
        var referenceTables = await QueryAsync(_reference,
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public'");

        Assert.Empty(_tables.Except(referenceTables));
    }

    [Fact]
    public Task Columns_match() => AssertSameAsync("""
        SELECT format('%s.%s %s%s%s%s', c.relname, a.attname, format_type(a.atttypid, a.atttypmod),
                      CASE WHEN a.attnotnull THEN ' NOT NULL' ELSE '' END,
                      CASE WHEN a.attidentity <> '' THEN ' IDENTITY ' || a.attidentity::text ELSE '' END,
                      coalesce(' DEFAULT ' || pg_get_expr(d.adbin, d.adrelid), ''))
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
        WHERE n.nspname = 'public' AND c.relname = ANY(@tables) AND a.attnum > 0 AND NOT a.attisdropped
        """);

    // Names differ (EF needs a name for every CHECK), so constraints are compared by definition.
    [Fact]
    public Task Check_constraints_match() => AssertSameAsync("""
        SELECT c.relname || ' ' || pg_get_constraintdef(con.oid)
        FROM pg_constraint con JOIN pg_class c ON c.oid = con.conrelid
        WHERE con.contype = 'c' AND c.relname = ANY(@tables)
        """);

    // A foreign key to a table of a later milestone is added with that table.
    [Fact]
    public Task Foreign_keys_match() => AssertSameAsync("""
        SELECT c.relname || ' ' || pg_get_constraintdef(con.oid)
        FROM pg_constraint con
        JOIN pg_class c ON c.oid = con.conrelid
        JOIN pg_class r ON r.oid = con.confrelid
        WHERE con.contype = 'f' AND c.relname = ANY(@tables) AND r.relname = ANY(@tables)
        """);

    // UNIQUE constraints in schema.sql are unique indexes in EF; both compare as index definitions.
    [Fact]
    public Task Indexes_match() => AssertSameAsync("""
        SELECT regexp_replace(indexdef, '^CREATE (UNIQUE )?INDEX \S+ ON ', 'CREATE \1INDEX ON ')
        FROM pg_indexes WHERE schemaname = 'public' AND tablename = ANY(@tables)
        """);

    [Fact]
    public async Task Sequences_used_by_the_model_exist_in_schema_sql()
    {
        const string sql = "SELECT sequencename FROM pg_sequences WHERE schemaname = 'public'";
        var migrated = await QueryAsync(_migrated, sql);
        var reference = await QueryAsync(_reference, sql);

        Assert.Empty(migrated.Except(reference));
    }

    private async Task AssertSameAsync(string sql)
    {
        var migrated = await QueryAsync(_migrated, sql);
        var reference = await QueryAsync(_reference, sql);

        var onlyInMigration = migrated.Except(reference).ToList();
        var onlyInSchemaSql = reference.Except(migrated).ToList();

        Assert.True(onlyInMigration.Count == 0 && onlyInSchemaSql.Count == 0,
            $"Only in migration:\n  {string.Join("\n  ", onlyInMigration)}\n" +
            $"Only in schema.sql:\n  {string.Join("\n  ", onlyInSchemaSql)}");
    }

    private async Task<List<string>> QueryAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tables", _tables);

        var rows = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(Whitespace().Replace(reader.GetString(0), " "));
        }

        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

/// <summary>One container per test class: a migrated database and one built from schema.sql.</summary>
public sealed class SchemaDatabases : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();

    public string Migrated { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string[] Tables { get; private set; } = [];

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        Migrated = await CreateDatabaseAsync("migrated");
        Reference = await CreateDatabaseAsync("reference");

        var options = new DbContextOptionsBuilder<AtsDbContext>();
        AtsDbContextOptions.Configure(options, Migrated);
        await using var context = new AtsDbContext(options.Options);
        await context.Database.MigrateAsync();
        Tables = [.. context.Model.GetEntityTypes().Select(e => e.GetTableName()!).Distinct().Order()];

        await using var connection = new NpgsqlConnection(Reference);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(FindSchemaSql()), connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    private async Task<string> CreateDatabaseAsync(string name)
    {
        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = name }.ConnectionString;
    }

    private static string FindSchemaSql()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "docs", "database", "schema.sql");
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("docs/database/schema.sql not found above " + AppContext.BaseDirectory);
    }
}
