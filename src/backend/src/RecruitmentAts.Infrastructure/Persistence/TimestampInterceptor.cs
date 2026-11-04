using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RecruitmentAts.Application.Abstractions.Time;

namespace RecruitmentAts.Infrastructure.Persistence;

/// <summary>
/// Sets <c>CreatedAt</c> (on insert, unless already set) and <c>UpdatedAt</c> (on insert and
/// update) from <see cref="IClock"/>, so services and tests never depend on the database clock.
/// </summary>
internal sealed class TimestampInterceptor(IClock clock) : SaveChangesInterceptor
{
    private const string CreatedAt = "CreatedAt";
    private const string UpdatedAt = "UpdatedAt";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Metadata.FindProperty(CreatedAt) is not null
                    && entry.Property(CreatedAt).CurrentValue is DateTimeOffset createdAt
                    && createdAt == default)
                {
                    entry.Property(CreatedAt).CurrentValue = now;
                }

                SetIfMapped(entry, UpdatedAt, now);
            }
            else if (entry.State == EntityState.Modified)
            {
                SetIfMapped(entry, UpdatedAt, now);
            }
        }
    }

    private static void SetIfMapped(EntityEntry entry, string property, DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(property) is not null)
        {
            entry.Property(property).CurrentValue = value;
        }
    }
}
