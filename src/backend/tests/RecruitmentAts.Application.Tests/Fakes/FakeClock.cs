using RecruitmentAts.Application.Abstractions.Time;

namespace RecruitmentAts.Application.Tests.Fakes;

public sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
