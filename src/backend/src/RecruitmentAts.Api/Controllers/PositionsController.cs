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
    [HttpGet(Name = "listPositions")]
    public Task<PagedResult<PositionSummary>> List([FromQuery] PositionQuery query, CancellationToken cancellationToken) =>
        positions.ListAsync(query, actors.Current(), cancellationToken);

    [HttpGet("{positionId:guid}", Name = "getPosition")]
    public Task<PositionDetail> Get(Guid positionId, CancellationToken cancellationToken) =>
        positions.GetAsync(positionId, actors.Current(), cancellationToken);
}
