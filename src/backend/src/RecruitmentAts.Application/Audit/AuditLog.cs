using System.Net;

namespace RecruitmentAts.Application.Audit;

/// <summary>Table <c>audit_logs</c>.</summary>
public sealed class AuditLog
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required AuditActorType ActorType { get; set; }
    public Guid? ActorId { get; set; }
    public required AuditModule Module { get; set; }
    public required AuditAction Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public required string Summary { get; set; }
    public string? Details { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? TraceId { get; set; }
}
