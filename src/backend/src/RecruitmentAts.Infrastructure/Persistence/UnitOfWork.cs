using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Infrastructure.Persistence;

internal sealed class UnitOfWork(AtsDbContext db) : IUnitOfWork
{
    public async Task ExecuteAsync(Func<Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        await ExecuteAsync(async () =>
        {
            await work();
            return true;
        }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Not committed → rolled back when the transaction is disposed.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await work();
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new AppException(
                ErrorCode.ConcurrencyConflict,
                "Dữ liệu đã được người khác thay đổi. Vui lòng tải lại trang và thử lại.",
                innerException: ex);
        }
    }
}
