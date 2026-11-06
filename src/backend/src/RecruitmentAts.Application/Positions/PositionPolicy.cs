using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Positions;

/// <summary>
/// Who may see and change a position (docs/02 role matrix, BR-14). The Api already enforces <c>x-roles</c>;
/// this adds the rules that depend on the position.
/// </summary>
public static class PositionPolicy
{
    /// <summary>
    /// An Interviewer sees only positions where they sit on an interview panel (AC-US04 #2). Panels arrive
    /// with US14, so until then an Interviewer sees none.
    /// </summary>
    public static bool SeesAllPositions(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.Role is Role.Admin or Role.HrManager or Role.Recruiter;
    }

    /// <summary>Not found rather than forbidden, so an Interviewer cannot learn which positions exist.</summary>
    public static void EnsureCanView(Actor actor)
    {
        if (!SeesAllPositions(actor))
        {
            throw PositionNotFound();
        }
    }

    internal static AppException PositionNotFound() => AppException.NotFound("Không tìm thấy vị trí tuyển dụng.");
}
