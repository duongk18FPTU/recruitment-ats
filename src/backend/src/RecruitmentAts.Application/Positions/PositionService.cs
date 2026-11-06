using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Application.Positions;

/// <summary>Positions and their weighted criteria (US04–US07).</summary>
public sealed class PositionService(
    IPositionRepository positions,
    IValidator<PositionQuery> queryValidator)
{
    public async Task<PagedResult<PositionSummary>> ListAsync(PositionQuery query, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        await queryValidator.ValidateOrThrowAsync(query, cancellationToken);
        if (!PositionPolicy.SeesAllPositions(actor))
        {
            return new PagedResult<PositionSummary>([], query.Page, query.PageSize, 0);
        }

        return await positions.ListAsync(query with { Q = query.Q?.Trim() }, cancellationToken);
    }

    public async Task<PositionDetail> GetAsync(Guid positionId, Actor actor, CancellationToken cancellationToken)
    {
        PositionPolicy.EnsureCanView(actor);
        return await positions.GetDetailAsync(positionId, cancellationToken) ?? throw PositionPolicy.PositionNotFound();
    }
}
