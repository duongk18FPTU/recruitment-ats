using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Lookups;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(AtsDbContext db) : IDepartmentRepository
{
    public async Task<IReadOnlyList<Department>> ListActiveAsync(CancellationToken cancellationToken) =>
        await db.Set<Department>().AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
}
