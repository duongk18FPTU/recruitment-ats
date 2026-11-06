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

    /// <summary>A Recruiter edits only positions they own (BR-14, AC-US06 #1).</summary>
    public static void EnsureCanEdit(Actor actor, Position position)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(position);
        if (actor.Role == Role.Recruiter && position.OwnerId != actor.UserId)
        {
            throw AppException.Forbidden("Bạn chỉ được sửa vị trí do mình phụ trách.");
        }
    }

    /// <summary>Only Admin and HR Manager reassign the owner (BR-14).</summary>
    public static void EnsureCanChangeOwner(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.Role is not (Role.Admin or Role.HrManager))
        {
            throw AppException.Forbidden("Chỉ Quản trị viên hoặc Trưởng tuyển dụng được đổi người phụ trách.");
        }
    }

    /// <summary>
    /// <c>DRAFT → OPEN</c>: Admin, HR Manager, or the owner. <c>OPEN → CLOSED</c> and <c>CLOSED → OPEN</c>:
    /// Admin and HR Manager only (US07, AC-US07 #2).
    /// </summary>
    public static void EnsureCanChangeStatus(Actor actor, Position position, PositionStatus target)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(position);
        if (actor.Role is Role.Admin or Role.HrManager)
        {
            return;
        }

        var ownerOpensDraft = position.Status == PositionStatus.Draft && target == PositionStatus.Open
            && actor.Role == Role.Recruiter && position.OwnerId == actor.UserId;
        if (!ownerOpensDraft)
        {
            throw AppException.Forbidden(position.Status == PositionStatus.Draft
                ? "Chỉ người phụ trách, Quản trị viên hoặc Trưởng tuyển dụng được mở tuyển vị trí này."
                : "Chỉ Quản trị viên hoặc Trưởng tuyển dụng được đóng hoặc mở lại vị trí.");
        }
    }

    internal static AppException PositionNotFound() => AppException.NotFound("Không tìm thấy vị trí tuyển dụng.");
}
