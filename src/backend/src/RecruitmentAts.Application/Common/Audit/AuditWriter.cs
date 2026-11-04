using System.Text.Json;
using System.Text.Json.Serialization;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Audit;

namespace RecruitmentAts.Application.Common.Audit;

/// <summary>
/// Adds an audit entry to the caller's unit of work, so the entry is saved in the same
/// transaction as the change it describes. <c>details</c> holds before/after values —
/// never secrets or CV text.
/// </summary>
public sealed class AuditWriter(IAuditLogRepository auditLogs, IClock clock)
{
    private const int SummaryMaxLength = 300;

    private static readonly JsonSerializerOptions DetailsJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) },
    };

    public void Write(
        Actor actor,
        AuditModule module,
        AuditAction action,
        string? entityType,
        Guid? entityId,
        string summary,
        object? details = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        auditLogs.Add(new AuditLog
        {
            OccurredAt = clock.UtcNow,
            ActorType = actor.Type,
            ActorId = actor.UserId,
            Module = module,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = summary.Length <= SummaryMaxLength ? summary : summary[..(SummaryMaxLength - 1)] + "…",
            Details = details is null ? null : JsonSerializer.Serialize(details, DetailsJson),
            IpAddress = actor.IpAddress,
            TraceId = actor.TraceId,
        });
    }
}
