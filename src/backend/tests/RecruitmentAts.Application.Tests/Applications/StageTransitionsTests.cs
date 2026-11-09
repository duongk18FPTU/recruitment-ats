using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Tests.Applications;

/// <summary>BR-01 to BR-03.</summary>
public class StageTransitionsTests
{
    [Theory]
    [InlineData(Stage.New, new[] { Stage.Screening, Stage.Rejected })]
    [InlineData(Stage.Screening, new[] { Stage.Interview, Stage.Rejected })]
    [InlineData(Stage.Interview, new[] { Stage.Offer, Stage.Rejected })]
    [InlineData(Stage.Offer, new[] { Stage.Hired, Stage.Rejected })]
    public void Admin_and_HR_Manager_move_one_step_forward_or_reject(Stage from, Stage[] expected)
    {
        Assert.Equal(expected, StageTransitions.AllowedNext(from, Role.Admin));
        Assert.Equal(expected, StageTransitions.AllowedNext(from, Role.HrManager));
    }

    [Theory]
    [InlineData(Stage.New, new[] { Stage.Screening, Stage.Rejected })]
    [InlineData(Stage.Screening, new[] { Stage.Interview, Stage.Rejected })]
    [InlineData(Stage.Interview, new[] { Stage.Rejected })]
    [InlineData(Stage.Offer, new[] { Stage.Rejected })]
    public void A_Recruiter_cannot_move_to_OFFER_or_HIRED(Stage from, Stage[] expected) =>
        Assert.Equal(expected, StageTransitions.AllowedNext(from, Role.Recruiter));

    [Theory]
    [InlineData(Stage.Hired)]
    [InlineData(Stage.Rejected)]
    public void Final_stages_have_no_next_stage(Stage from)
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            Assert.Empty(StageTransitions.AllowedNext(from, role));
        }
    }

    [Theory]
    [InlineData(Stage.New)]
    [InlineData(Stage.Interview)]
    public void An_Interviewer_or_unknown_role_moves_nothing(Stage from)
    {
        Assert.Empty(StageTransitions.AllowedNext(from, Role.Interviewer));
        Assert.Empty(StageTransitions.AllowedNext(from, null));
    }
}
