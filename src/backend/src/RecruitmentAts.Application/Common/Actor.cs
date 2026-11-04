using System.Net;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Common;

/// <summary>
/// Who performs a use case. Every service method receives it; audit entries copy it.
/// Built by the Api (<c>ActorAccessor</c>) or the Worker (<see cref="System"/>).
/// </summary>
public sealed record Actor(Guid? UserId, Role? Role, AuditActorType Type, IPAddress? IpAddress, string TraceId)
{
    public static Actor User(Guid userId, Role role, IPAddress? ipAddress, string traceId) =>
        new(userId, role, AuditActorType.User, ipAddress, traceId);

    public static Actor System(string traceId) => new(null, null, AuditActorType.System, null, traceId);

    /// <summary>An anonymous caller of the public API (career site, consent links).</summary>
    public static Actor Candidate(IPAddress? ipAddress, string traceId) =>
        new(null, null, AuditActorType.Candidate, ipAddress, traceId);
}
