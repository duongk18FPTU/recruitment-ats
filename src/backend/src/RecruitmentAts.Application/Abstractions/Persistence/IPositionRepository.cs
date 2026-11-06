using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IPositionRepository
{
    /// <summary>Filtered, sorted page; <c>q</c> matches title or code, ignoring case.</summary>
    Task<PagedResult<PositionSummary>> ListAsync(PositionQuery query, CancellationToken cancellationToken);

    /// <summary>The tracked position with the rows of its current criteria version, or null.</summary>
    Task<Position?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The position with its current criteria version, or null.</summary>
    Task<PositionDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    void Add(Position position);
}
