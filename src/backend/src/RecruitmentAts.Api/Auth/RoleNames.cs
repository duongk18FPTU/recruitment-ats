namespace RecruitmentAts.Api.Auth;

/// <summary>
/// Values of the access-token claim <c>role</c>, for <c>[Authorize(Roles = …)]</c> from the
/// operation's <c>x-roles</c>. A role that is not listed gets 403 <c>FORBIDDEN</c>.
/// </summary>
public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string HrManager = "HR_MANAGER";
    public const string Recruiter = "RECRUITER";
    public const string Interviewer = "INTERVIEWER";
}
