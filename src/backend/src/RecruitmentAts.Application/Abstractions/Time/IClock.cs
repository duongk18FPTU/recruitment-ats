namespace RecruitmentAts.Application.Abstractions.Time;

/// <summary>Current time in UTC; replaced by FakeClock in tests.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
