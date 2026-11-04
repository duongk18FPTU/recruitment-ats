using RecruitmentAts.Application.Abstractions.Time;

namespace RecruitmentAts.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
