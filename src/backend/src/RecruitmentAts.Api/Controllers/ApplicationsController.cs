using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Applications.Contracts;
using RecruitmentAts.Application.Candidates;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(ApplicationService applications, ActorAccessor actors) : ControllerBase
{
    private const string HiringTeam = RoleNames.Admin + "," + RoleNames.HrManager + "," + RoleNames.Recruiter;

    /// <summary>
    /// <c>multipart/form-data</c>. The file is read at most up to 10 MB + 1 byte by the service, which answers
    /// 413 <c>FILE_TOO_LARGE</c>; the request body limit only guards against absurd uploads.
    /// </summary>
    [HttpPost(Name = "createApplication")]
    [Authorize(Roles = HiringTeam)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CvFileRules.MaxSizeBytes + 1024 * 1024)]
    public async Task<ActionResult<ApplicationCreated>> Create([FromForm] ApplicationCreateForm form, CancellationToken cancellationToken)
    {
        await using var content = form.Cv?.OpenReadStream();
        var cv = form.Cv is null ? null : new CvUpload(form.Cv.FileName, form.Cv.ContentType, content!);
        var created = await applications.CreateAsync(
            new ApplicationCreateRequest(form.PositionId, form.FullName, form.Email, form.Phone, form.Source, form.SourceNote, form.Notes, cv),
            actors.Current(),
            cancellationToken);
        return Created($"/api/applications/{created.Application.Id}", created);
    }
}

/// <summary>OpenAPI <c>ApplicationCreateForm</c>; the file is turned into a <see cref="CvUpload"/> by the controller.</summary>
public sealed class ApplicationCreateForm
{
    public Guid? PositionId { get; init; }

    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public Source? Source { get; init; }

    public string? SourceNote { get; init; }

    public string? Notes { get; init; }

    public IFormFile? Cv { get; init; }
}
