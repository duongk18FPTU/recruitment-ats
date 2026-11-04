using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Audit;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(AtsDbContext db) : IAuditLogRepository
{
    public void Add(AuditLog entry) => db.Set<AuditLog>().Add(entry);
}
