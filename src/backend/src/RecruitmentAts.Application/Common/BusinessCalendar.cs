using RecruitmentAts.Application.Abstractions.Time;

namespace RecruitmentAts.Application.Common;

/// <summary>Calendar dates in the company's time zone, Asia/Ho_Chi_Minh (UTC+7 all year, no daylight saving).</summary>
public static class BusinessCalendar
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateOnly Today(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return DateOnly.FromDateTime(clock.UtcNow.ToOffset(Offset).DateTime);
    }
}
