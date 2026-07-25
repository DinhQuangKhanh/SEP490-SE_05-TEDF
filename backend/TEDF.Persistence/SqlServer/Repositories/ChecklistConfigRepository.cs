using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories;

/// <summary>Repository implementation for the <see cref="ChecklistConfig"/> aggregate.</summary>
public class ChecklistConfigRepository : BaseRepository<ChecklistConfig, Guid>, IChecklistConfigRepository
{
    public ChecklistConfigRepository(AppDbContext context) : base(context) { }

    public override async Task<ChecklistConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ChecklistConfig?> GetActiveBySemesterAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.SemesterId == semesterId && c.Status == ChecklistConfigStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistConfig>> GetBySemesterAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Criteria)
            .Where(c => c.SemesterId == semesterId)
            .OrderByDescending(c => c.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistConfig>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Criteria)
            .OrderByDescending(c => c.SemesterId)
            .ThenByDescending(c => c.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsActiveForSemesterAsync(int semesterId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            c => c.SemesterId == semesterId
                 && c.Status == ChecklistConfigStatus.Active
                 && (excludeId == null || c.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task<int> GetMaxVersionForSemesterAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.SemesterId == semesterId)
            .Select(c => (int?)c.Version)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task<bool> HasResultsAsync(Guid checklistConfigId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProjectEvaluationChecklist>()
            .AnyAsync(r => r.ChecklistConfigId == checklistConfigId, cancellationToken);
    }
}
