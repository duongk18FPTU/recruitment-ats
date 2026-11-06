using RecruitmentAts.Application.Lookups;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IDepartmentRepository
{
    /// <summary>Active departments in their display order.</summary>
    Task<IReadOnlyList<Department>> ListActiveAsync(CancellationToken cancellationToken);

    Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken);
}
