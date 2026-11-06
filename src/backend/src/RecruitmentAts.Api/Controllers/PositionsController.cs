using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(PositionService positions, ActorAccessor actors) : ControllerBase
{
    private const string HiringTeam = RoleNames.Admin + "," + RoleNames.HrManager + "," + RoleNames.Recruiter;

    [HttpGet(Name = "listPositions")]
    public Task<PagedResult<PositionSummary>> List([FromQuery] PositionQuery query, CancellationToken cancellationToken) =>
        positions.ListAsync(query, actors.Current(), cancellationToken);

    [HttpPost(Name = "createPosition")]
    [Authorize(Roles = HiringTeam)]
    public async Task<ActionResult<PositionDetail>> Create(PositionCreateRequest request, CancellationToken cancellationToken)
    {
        var position = await positions.CreateAsync(request, actors.Current(), cancellationToken);
        return CreatedAtRoute("getPosition", new { positionId = position.Id }, position);
    }

    [HttpGet("{positionId:guid}", Name = "getPosition")]
    public Task<PositionDetail> Get(Guid positionId, CancellationToken cancellationToken) =>
        positions.GetAsync(positionId, actors.Current(), cancellationToken);

    [HttpPut("{positionId:guid}", Name = "updatePosition")]
    [Authorize(Roles = HiringTeam)]
    public Task<PositionDetail> Update(Guid positionId, PositionUpdateRequest request, CancellationToken cancellationToken) =>
        positions.UpdateAsync(positionId, request, actors.Current(), cancellationToken);

    [HttpPut("{positionId:guid}/status", Name = "updatePositionStatus")]
    [Authorize(Roles = HiringTeam)]
    public Task<PositionDetail> UpdateStatus(Guid positionId, PositionStatusRequest request, CancellationToken cancellationToken) =>
        positions.ChangeStatusAsync(positionId, request, actors.Current(), cancellationToken);
}
