using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Application.Positions;

/// <summary>Positions and their weighted criteria (US04–US07).</summary>
public sealed class PositionService(
    IPositionRepository positions,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IValidator<PositionQuery> queryValidator,
    IValidator<PositionCreateRequest> createValidator)
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

    /// <summary>
    /// The caller becomes the owner (AC-US05 #1). The draft job post prefilled from the position
    /// (UC05) is created by US22.
    /// </summary>
    public async Task<PositionDetail> CreateAsync(PositionCreateRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var positionId = await unitOfWork.ExecuteAsync(async () =>
        {
            await EnsureDepartmentActiveAsync(request.DepartmentId!.Value, cancellationToken);
            var position = Position.Create(request, request.Status ?? PositionStatus.Draft, actor.UserId!.Value);
            positions.Add(position);
            audit.Write(actor, AuditModule.Position, AuditAction.Create, "Position", position.Id, $"Tạo vị trí {position.Title}",
                new { position.Title, position.Status, position.DepartmentId, criteria = position.Criteria.Count });
            return position.Id;
        }, cancellationToken);

        return await positions.GetDetailAsync(positionId, cancellationToken) ?? throw PositionPolicy.PositionNotFound();
    }

    private async Task EnsureDepartmentActiveAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (!await departments.IsActiveAsync(departmentId, cancellationToken))
        {
            throw AppException.Validation(new Dictionary<string, string[]>
            {
                ["departmentId"] = ["Phòng ban không tồn tại hoặc đã ngừng hoạt động."],
            });
        }
    }
}
