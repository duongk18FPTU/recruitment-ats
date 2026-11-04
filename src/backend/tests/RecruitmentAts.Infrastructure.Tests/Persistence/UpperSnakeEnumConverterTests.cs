using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Persistence;

namespace RecruitmentAts.Infrastructure.Tests.Persistence;

public class UpperSnakeEnumConverterTests
{
    [Fact]
    public void Converts_to_the_values_in_schema_sql()
    {
        Assert.Equal("HR_MANAGER", UpperSnakeEnumConverter<Role>.ToDatabase(Role.HrManager));
        Assert.Equal("TOPCV", UpperSnakeEnumConverter<Source>.ToDatabase(Source.Topcv));
        Assert.Equal("VIETNAMWORKS", UpperSnakeEnumConverter<Source>.ToDatabase(Source.Vietnamworks));
        Assert.Equal("NOT_MET", UpperSnakeEnumConverter<CriterionOutcome>.ToDatabase(CriterionOutcome.NotMet));
        Assert.Equal("CV_EMBEDDING", UpperSnakeEnumConverter<AiCallPurpose>.ToDatabase(AiCallPurpose.CvEmbedding));
        Assert.Equal("AI_OVERRIDE", UpperSnakeEnumConverter<AuditAction>.ToDatabase(AuditAction.AiOverride));
    }

    [Fact]
    public void Every_value_round_trips()
    {
        AssertRoundTrips<Role>();
        AssertRoundTrips<Stage>();
        AssertRoundTrips<Source>();
        AssertRoundTrips<RejectionReason>();
        AssertRoundTrips<CriterionOutcome>();
        AssertRoundTrips<AiCallPurpose>();
        AssertRoundTrips<AuditModule>();
        AssertRoundTrips<AuditAction>();
    }

    private static void AssertRoundTrips<TEnum>()
        where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var text = UpperSnakeEnumConverter<TEnum>.ToDatabase(value);
            Assert.Equal(value, UpperSnakeEnumConverter<TEnum>.FromDatabase(text));
        }
    }
}
