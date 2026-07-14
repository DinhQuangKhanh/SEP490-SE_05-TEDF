using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Persistence.SqlServer.Repositories;

/// <summary>Repository implementation for the <see cref="ProjectEvaluationChecklist"/> aggregate.</summary>
public class ProjectEvaluationChecklistRepository : IProjectEvaluationChecklistRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<ProjectEvaluationChecklist> _dbSet;

    public ProjectEvaluationChecklistRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<ProjectEvaluationChecklist>();
    }

    public async Task AddAsync(ProjectEvaluationChecklist checklist, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(checklist, cancellationToken);
    }

    public void Update(ProjectEvaluationChecklist checklist)
    {
        _dbSet.Update(checklist);
    }

    public async Task<ProjectEvaluationChecklist?> GetByProjectEvaluatorAsync(
        Guid projectId, Guid evaluatorId, int submissionNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.ProjectId == projectId && c.EvaluatorId == evaluatorId && c.SubmissionNumber == submissionNumber,
                cancellationToken);
    }

    public async Task<ProjectEvaluationChecklist?> GetLatestByProjectEvaluatorAsync(
        Guid projectId, Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Items)
            .Where(c => c.ProjectId == projectId && c.EvaluatorId == evaluatorId)
            .OrderByDescending(c => c.SubmissionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectEvaluationChecklist>> GetByProjectAsync(
        Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Items)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.SubmissionNumber)
            .ToListAsync(cancellationToken);
    }
}
