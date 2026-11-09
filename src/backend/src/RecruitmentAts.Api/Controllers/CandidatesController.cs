using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Candidates.Contracts;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/candidates")]
public sealed class CandidatesController(CandidateService candidates) : ControllerBase
{
    private const string HiringTeam = RoleNames.Admin + "," + RoleNames.HrManager + "," + RoleNames.Recruiter;

    [HttpGet("match", Name = "matchCandidate")]
    [Authorize(Roles = HiringTeam)]
    public Task<CandidateMatch> Match(
        [FromQuery] string? email, [FromQuery] string? phone, [FromQuery] Guid? positionId, CancellationToken cancellationToken) =>
        candidates.MatchAsync(new CandidateMatchQuery(email, phone, positionId), cancellationToken);
}
