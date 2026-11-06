using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Lookups.Contracts;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public sealed class LookupsController(LookupService lookups) : ControllerBase
{
    [HttpGet("departments", Name = "listDepartments")]
    public Task<IReadOnlyList<DepartmentOption>> Departments(CancellationToken cancellationToken) =>
        lookups.DepartmentsAsync(cancellationToken);

    [HttpGet("users", Name = "listUserOptions")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.HrManager + "," + RoleNames.Recruiter)]
    public Task<IReadOnlyList<UserOption>> Users([FromQuery] UserOptionQuery query, CancellationToken cancellationToken) =>
        lookups.UsersAsync(query, cancellationToken);
}
