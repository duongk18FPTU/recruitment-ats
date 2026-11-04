using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RecruitmentAts.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c>; reads <c>ConnectionStrings__Ats</c> like the hosts do.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AtsDbContext>
{
    private const string LocalDefault = "Host=localhost;Port=5433;Database=ats;Username=ats";

    public AtsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Ats") ?? LocalDefault;
        var options = new DbContextOptionsBuilder<AtsDbContext>();
        AtsDbContextOptions.Configure(options, connectionString);
        return new AtsDbContext(options.Options);
    }
}
