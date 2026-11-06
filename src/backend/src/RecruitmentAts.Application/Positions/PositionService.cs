using System.Globalization;
using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Positions.Contracts;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Positions;

/// <summary>Positions and their weighted criteria (US04–US07).</summary>
public sealed class PositionService(
    IPositionRepository positions,
    IDepartmentRepository departments,
    IUserRepository users,
    IAnalysisRepository analyses,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IValidator<PositionQuery> queryValidator,
    IValidator<PositionCreateRequest> createValidator,
    IValidator<PositionUpdateRequest> updateValidator)
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

    /// <summary>
    /// Recruiters edit only their own positions; only Admin and HR Manager change the owner (BR-14).
    /// Changed criteria become a new version and older completed analyses become <c>OUTDATED</c>
    /// (AC-US06). A stale <c>rowVersion</c> is 409 <c>CONCURRENCY_CONFLICT</c>.
    /// </summary>
    public async Task<PositionDetail> UpdateAsync(
        Guid positionId, PositionUpdateRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        await unitOfWork.ExecuteAsync(async () =>
        {
            var position = await positions.GetAsync(positionId, cancellationToken) ?? throw PositionPolicy.PositionNotFound();
            PositionPolicy.EnsureCanEdit(actor, position);
            if (position.RowVersion != uint.Parse(request.RowVersion, CultureInfo.InvariantCulture))
            {
                throw StaleVersion();
            }

            var newOwner = request.OwnerId is { } ownerId && ownerId != position.OwnerId ? ownerId : (Guid?)null;
            if (newOwner is not null)
            {
                PositionPolicy.EnsureCanChangeOwner(actor);
                await EnsureOwnerEligibleAsync(newOwner.Value, cancellationToken);
            }

            if (request.DepartmentId != position.DepartmentId)
            {
                await EnsureDepartmentActiveAsync(request.DepartmentId!.Value, cancellationToken);
            }

            EnsureCurrentCriterionIds(position, request.Criteria!);

            var before = Snapshot(position);
            position.Update(request);
            position.OwnerId = newOwner ?? position.OwnerId;
            var criteriaChanged = position.ReplaceCriteria(request.Criteria!);
            var outdated = criteriaChanged
                ? await analyses.MarkOutdatedAsync(position.Id, position.CriteriaVersion, cancellationToken)
                : 0;

            audit.Write(actor, AuditModule.Position, AuditAction.Update, "Position", position.Id,
                criteriaChanged
                    ? $"Sửa vị trí {position.Code} {position.Title}, tiêu chí phiên bản {position.CriteriaVersion}"
                    : $"Sửa vị trí {position.Code} {position.Title}",
                new { before, after = Snapshot(position), criteriaChanged, outdatedAnalyses = outdated });
            return position.Id;
        }, cancellationToken);

        return await positions.GetDetailAsync(positionId, cancellationToken) ?? throw PositionPolicy.PositionNotFound();
    }

    private static object Snapshot(Position p) => new
    {
        p.Title, p.DepartmentId, p.Location, p.EmploymentType, p.Level, p.Headcount, p.TargetHireDate, p.OwnerId,
        p.CriteriaVersion,
    };

    /// <summary>An <c>id</c> in the request must name a criterion of the current version.</summary>
    private static void EnsureCurrentCriterionIds(Position position, IReadOnlyList<CriterionInput> inputs)
    {
        var current = position.CurrentCriteria().Select(c => c.Id).ToHashSet();
        var errors = inputs
            .Select((input, index) => (input, index))
            .Where(x => x.input.Id is { } id && !current.Contains(id))
            .ToDictionary(x => $"criteria[{x.index}].id", _ => new[] { "Tiêu chí này không thuộc phiên bản hiện tại; tải lại vị trí." });
        if (errors.Count > 0)
        {
            throw AppException.Validation(errors);
        }
    }

    private async Task EnsureOwnerEligibleAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var owner = await users.FindAsync(ownerId, cancellationToken);
        if (owner is not { IsActive: true, Role: Role.Admin or Role.HrManager or Role.Recruiter })
        {
            throw AppException.Validation(new Dictionary<string, string[]>
            {
                ["ownerId"] = ["Người phụ trách phải là Quản trị viên, Trưởng tuyển dụng hoặc Chuyên viên tuyển dụng đang hoạt động."],
            });
        }
    }

    private static AppException StaleVersion() => new(
        ErrorCode.ConcurrencyConflict, "Vị trí vừa được người khác thay đổi. Tải lại để xem bản mới nhất rồi sửa lại.");

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
