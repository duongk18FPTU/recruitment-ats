using System.Net;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Tests.Fakes;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Tests.Common.Audit;

public class AuditWriterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 30, 0, TimeSpan.Zero);

    private readonly FakeAuditLogs _auditLogs = new();
    private readonly AuditWriter _writer;

    public AuditWriterTests() => _writer = new AuditWriter(_auditLogs, new FakeClock(Now));

    [Fact]
    public void Copies_the_user_actor_and_uses_the_clock()
    {
        var userId = Guid.CreateVersion7();
        var entityId = Guid.CreateVersion7();
        var actor = Actor.User(userId, Role.Recruiter, IPAddress.Parse("10.0.0.7"), "trace-1");

        _writer.Write(actor, AuditModule.Application, AuditAction.StageChange, "Application", entityId, "Moved to SCREENING");

        var entry = Assert.Single(_auditLogs.Entries);
        Assert.Equal(Now, entry.OccurredAt);
        Assert.Equal(AuditActorType.User, entry.ActorType);
        Assert.Equal(userId, entry.ActorId);
        Assert.Equal(AuditModule.Application, entry.Module);
        Assert.Equal(AuditAction.StageChange, entry.Action);
        Assert.Equal("Application", entry.EntityType);
        Assert.Equal(entityId, entry.EntityId);
        Assert.Equal("Moved to SCREENING", entry.Summary);
        Assert.Null(entry.Details);
        Assert.Equal(IPAddress.Parse("10.0.0.7"), entry.IpAddress);
        Assert.Equal("trace-1", entry.TraceId);
    }

    [Fact]
    public void System_and_candidate_actors_have_no_user_id()
    {
        _writer.Write(Actor.System("t1"), AuditModule.Candidate, AuditAction.Anonymize, null, null, "Retention");
        _writer.Write(Actor.Candidate(IPAddress.Loopback, "t2"), AuditModule.Consent, AuditAction.ConsentChange, null, null, "Withdrawn");

        Assert.Collection(
            _auditLogs.Entries,
            system =>
            {
                Assert.Equal(AuditActorType.System, system.ActorType);
                Assert.Null(system.ActorId);
                Assert.Null(system.IpAddress);
            },
            candidate =>
            {
                Assert.Equal(AuditActorType.Candidate, candidate.ActorType);
                Assert.Null(candidate.ActorId);
                Assert.Equal(IPAddress.Loopback, candidate.IpAddress);
            });
    }

    [Fact]
    public void Details_are_json_with_camel_case_names_and_stored_enum_values()
    {
        _writer.Write(Actor.System("t"), AuditModule.Application, AuditAction.StageChange, null, null, "s",
            new { FromStage = Stage.New, ToStage = Stage.Screening });

        Assert.Equal("""{"fromStage":"NEW","toStage":"SCREENING"}""", _auditLogs.Entries[0].Details);
    }

    [Fact]
    public void Long_summary_is_cut_to_the_column_length()
    {
        _writer.Write(Actor.System("t"), AuditModule.Report, AuditAction.Export, null, null, new string('x', 500));

        var summary = _auditLogs.Entries[0].Summary;
        Assert.Equal(300, summary.Length);
        Assert.EndsWith("…", summary, StringComparison.Ordinal);
    }

    private sealed class FakeAuditLogs : IAuditLogRepository
    {
        public List<AuditLog> Entries { get; } = [];

        public void Add(AuditLog entry) => Entries.Add(entry);
    }
}
