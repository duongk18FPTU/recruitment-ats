using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Applications;

/// <summary>The pipeline rules BR-01 to BR-03.</summary>
public static class StageTransitions
{
    /// <summary>
    /// Stages an application in <paramref name="from"/> may move to now, for <paramref name="role"/>: one step
    /// forward (no skipping), or <c>REJECTED</c> from any non-final stage; <c>HIRED</c> and <c>REJECTED</c> are
    /// final. Only Admin and HR Manager move to <c>OFFER</c> or <c>HIRED</c>; an Interviewer moves nothing.
    /// </summary>
    public static IReadOnlyList<Stage> AllowedNext(Stage from, Role? role)
    {
        if (role is not (Role.Admin or Role.HrManager or Role.Recruiter) || from is Stage.Hired or Stage.Rejected)
        {
            return [];
        }

        var forward = from switch
        {
            Stage.New => Stage.Screening,
            Stage.Screening => Stage.Interview,
            Stage.Interview => Stage.Offer,
            _ => Stage.Hired,
        };

        var mayDecide = role is Role.Admin or Role.HrManager;
        return forward is Stage.Offer or Stage.Hired && !mayDecide
            ? [Stage.Rejected]
            : [forward, Stage.Rejected];
    }
}
