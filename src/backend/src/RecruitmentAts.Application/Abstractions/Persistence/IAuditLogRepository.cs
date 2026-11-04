using RecruitmentAts.Application.Audit;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    /// <summary>Tracks the entry; it is saved by the surrounding <see cref="IUnitOfWork"/>.</summary>
    void Add(AuditLog entry);
}
