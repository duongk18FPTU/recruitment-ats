using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Lookups.Contracts;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Positions.Contracts;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class PositionRepository(AtsDbContext db) : IPositionRepository
{
    public async Task<PagedResult<PositionSummary>> ListAsync(PositionQuery query, CancellationToken cancellationToken)
    {
        var positions = db.Set<Position>().AsNoTracking();
        if (!string.IsNullOrEmpty(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            positions = positions.Where(p => EF.Functions.ILike(p.Title, pattern, LikePattern.Escape)
                || EF.Functions.ILike(p.Code, pattern, LikePattern.Escape));
        }

        if (query.Status is { } status)
        {
            positions = positions.Where(p => p.Status == status);
        }

        if (query.DepartmentId is { } departmentId)
        {
            positions = positions.Where(p => p.DepartmentId == departmentId);
        }

        if (query.OwnerId is { } ownerId)
        {
            positions = positions.Where(p => p.OwnerId == ownerId);
        }

        if (query.JobPostStatus is not null)
        {
            positions = positions.Where(_ => false); // no position has a job post before US22
        }

        var total = await positions.CountAsync(cancellationToken);
        var rows = await Sort(Rows(positions), query.Sort ?? PositionQuery.DefaultSort)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<PositionSummary>([.. rows.Select(Summary)], query.Page, query.PageSize, total);
    }

    public async Task<PositionDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await Rows(db.Set<Position>().AsNoTracking().Where(p => p.Id == id)).SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var p = row.Position;
        var criteria = await db.Set<PositionCriterion>().AsNoTracking()
            .Where(c => c.PositionId == id && c.Version == p.CriteriaVersion)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new CriterionView(c.Id, c.Name, c.Description, c.Type, c.Weight, c.Version))
            .ToListAsync(cancellationToken);
        var stageCounts = await db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.PositionId == id)
            .GroupBy(a => a.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new PositionDetail(
            p.Id, p.Code, p.Title, new DepartmentOption(p.DepartmentId, row.DepartmentName), p.Location, p.Status,
            new UserOption(p.OwnerId, row.OwnerName, row.OwnerRole), p.Headcount, p.TargetHireDate,
            row.ApplicationCount, row.HiredCount, p.EmploymentType, p.Level, p.Description, p.CriteriaVersion, criteria,
            [.. stageCounts.OrderBy(s => s.Stage).Select(s => new StageCount(s.Stage, s.Count))],
            p.CreatedAt, p.ClosedAt, p.RowVersion.ToString(CultureInfo.InvariantCulture));
    }

    public async Task<Position?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var position = await db.Set<Position>().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (position is not null)
        {
            // Loading the current rows fixes them up into position.Criteria.
            await db.Set<PositionCriterion>()
                .Where(c => c.PositionId == id && c.Version == position.CriteriaVersion)
                .LoadAsync(cancellationToken);
        }

        return position;
    }

    public void Add(Position position) => db.Set<Position>().Add(position);

    private IQueryable<Row> Rows(IQueryable<Position> positions) =>
        from p in positions
        join d in db.Set<Department>() on p.DepartmentId equals d.Id
        join o in db.Set<User>() on p.OwnerId equals o.Id
        select new Row
        {
            Position = p,
            DepartmentName = d.Name,
            OwnerName = o.FullName,
            OwnerRole = o.Role,
            ApplicationCount = db.Set<JobApplication>().Count(a => a.PositionId == p.Id),
            HiredCount = db.Set<JobApplication>().Count(a => a.PositionId == p.Id && a.Stage == Stage.Hired),
        };

    private static IQueryable<Row> Sort(IQueryable<Row> rows, string sort) => sort switch
    {
        "createdAt" => rows.OrderBy(r => r.Position.CreatedAt).ThenBy(r => r.Position.Id),
        "targetHireDate" => rows.OrderBy(r => r.Position.TargetHireDate).ThenBy(r => r.Position.Id),
        "-targetHireDate" => rows.OrderByDescending(r => r.Position.TargetHireDate).ThenBy(r => r.Position.Id),
        "title" => rows.OrderBy(r => r.Position.Title).ThenBy(r => r.Position.Id),
        _ => rows.OrderByDescending(r => r.Position.CreatedAt).ThenByDescending(r => r.Position.Id),
    };

    private static PositionSummary Summary(Row row)
    {
        var p = row.Position;
        return new PositionSummary(
            p.Id, p.Code, p.Title, new DepartmentOption(p.DepartmentId, row.DepartmentName), p.Location, p.Status,
            new UserOption(p.OwnerId, row.OwnerName, row.OwnerRole), p.Headcount, p.TargetHireDate,
            row.ApplicationCount, row.HiredCount);
    }

    private sealed class Row
    {
        public required Position Position { get; init; }
        public required string DepartmentName { get; init; }
        public required string OwnerName { get; init; }
        public required Role OwnerRole { get; init; }
        public required int ApplicationCount { get; init; }
        public required int HiredCount { get; init; }
    }
}
