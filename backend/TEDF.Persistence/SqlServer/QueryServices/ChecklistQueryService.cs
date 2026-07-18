using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>Read-side queries + DTO mapping for the topic-evaluation checklist feature.</summary>
public class ChecklistQueryService : IChecklistQueryService
{
    private readonly AppDbContext _context;

    public ChecklistQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectChecklistDto?> GetProjectChecklistAsync(
        Guid projectId, Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        // Only an actively assigned evaluator may view the checklist for this project.
        var isAssigned = await _context.ProjectEvaluatorAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectId == projectId && a.EvaluatorId == evaluatorId && a.IsActive, cancellationToken);
        if (!isAssigned)
            return null;

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.SemesterId, p.EvaluationCount })
            .FirstOrDefaultAsync(cancellationToken);
        if (project is null)
            return null;

        var submissionNumber = project.EvaluationCount;

        // A previously saved result for the current round takes precedence (returns the exact version used).
        var saved = await _context.ProjectEvaluationChecklists
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.ProjectId == projectId && c.EvaluatorId == evaluatorId && c.SubmissionNumber == submissionNumber,
                cancellationToken);

        if (saved is not null)
        {
            // Enrich the snapshot rows with English title / description from the exact config version used.
            var usedConfig = await _context.ChecklistConfigs
                .AsNoTracking()
                .Include(c => c.Criteria)
                .FirstOrDefaultAsync(c => c.Id == saved.ChecklistConfigId, cancellationToken);
            var criteriaById = usedConfig?.Criteria.ToDictionary(c => c.Id)
                ?? new Dictionary<Guid, ChecklistCriterion>();

            var savedItems = saved.Items
                .OrderBy(i => i.Order)
                .Select(i =>
                {
                    criteriaById.TryGetValue(i.CriterionId, out var criterion);
                    return new ProjectChecklistItemDto(
                        i.CriterionId, i.Order, i.TitleVi,
                        criterion?.TitleEn ?? string.Empty, criterion?.Description, i.IsPassed);
                })
                .ToList();

            return new ProjectChecklistDto(
                ProjectId: projectId,
                HasActiveConfig: true,
                ConfigId: saved.ChecklistConfigId,
                TotalCriteria: savedItems.Count,
                RequiredPassCount: saved.RequiredPassCount,
                PassedCount: saved.PassedCount,
                CanApprove: saved.MeetsApprovalThreshold,
                IsSaved: true,
                EvaluatorNote: saved.EvaluatorNote,
                UpdatedAt: saved.UpdatedAt,
                Items: savedItems);
        }

        // No saved result yet: build an initial (unsaved) view from the semester's Active config.
        var config = await _context.ChecklistConfigs
            .AsNoTracking()
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.SemesterId == project.SemesterId && c.Status == ChecklistConfigStatus.Active, cancellationToken);

        if (config is null)
        {
            return new ProjectChecklistDto(
                ProjectId: projectId,
                HasActiveConfig: false,
                ConfigId: null,
                TotalCriteria: 0,
                RequiredPassCount: ChecklistConfig.DefaultPassThreshold,
                PassedCount: 0,
                CanApprove: false,
                IsSaved: false,
                EvaluatorNote: null,
                UpdatedAt: null,
                Items: []);
        }

        var items = config.Criteria
            .OrderBy(c => c.Order)
            .Select(c => new ProjectChecklistItemDto(c.Id, c.Order, c.TitleVi, c.TitleEn, c.Description, IsPassed: false))
            .ToList();

        return new ProjectChecklistDto(
            ProjectId: projectId,
            HasActiveConfig: true,
            ConfigId: config.Id,
            TotalCriteria: items.Count,
            RequiredPassCount: config.PassThreshold,
            PassedCount: 0,
            CanApprove: false,
            IsSaved: false,
            EvaluatorNote: null,
            UpdatedAt: null,
            Items: items);
    }

    public async Task<ChecklistConfigListDto> GetConfigsAsync(int? semesterId, CancellationToken cancellationToken = default)
    {
        var semesters = await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .ToListAsync(cancellationToken);

        var semesterOptions = semesters
            .Select(s => new ChecklistSemesterOptionDto(s.Id, s.Name, s.Code.Value, s.Status.ToString()))
            .ToList();
        var semesterNameById = semesters.ToDictionary(s => s.Id, s => s.Name);

        var query = _context.ChecklistConfigs.AsNoTracking().Include(c => c.Criteria).AsQueryable();
        if (semesterId.HasValue)
            query = query.Where(c => c.SemesterId == semesterId.Value);

        var configs = await query
            .OrderByDescending(c => c.SemesterId)
            .ThenByDescending(c => c.Version)
            .ToListAsync(cancellationToken);

        var usedConfigIds = await GetUsedConfigIdsAsync(configs.Select(c => c.Id), cancellationToken);
        var userNames = await ResolveUserNamesAsync(configs, cancellationToken);

        var configDtos = configs
            .Select(c => MapConfig(c, Lookup(semesterNameById, c.SemesterId), usedConfigIds.Contains(c.Id), userNames))
            .ToList();

        return new ChecklistConfigListDto(semesterOptions, configDtos);
    }

    public async Task<ChecklistConfigDto?> GetConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _context.ChecklistConfigs
            .AsNoTracking()
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (config is null)
            return null;

        var semesterName = await _context.Semesters
            .AsNoTracking()
            .Where(s => s.Id == config.SemesterId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? $"#{config.SemesterId}";

        var usedConfigIds = await GetUsedConfigIdsAsync([config.Id], cancellationToken);
        var userNames = await ResolveUserNamesAsync([config], cancellationToken);

        return MapConfig(config, semesterName, usedConfigIds.Contains(config.Id), userNames);
    }

    private async Task<HashSet<Guid>> GetUsedConfigIdsAsync(IEnumerable<Guid> configIds, CancellationToken cancellationToken)
    {
        var ids = configIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var used = await _context.ProjectEvaluationChecklists
            .AsNoTracking()
            .Where(r => ids.Contains(r.ChecklistConfigId))
            .Select(r => r.ChecklistConfigId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return used.ToHashSet();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveUserNamesAsync(
        IReadOnlyList<ChecklistConfig> configs, CancellationToken cancellationToken)
    {
        var ids = configs
            .SelectMany(c => new[] { c.CreatedBy, c.UpdatedBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    private static ChecklistConfigDto MapConfig(
        ChecklistConfig config, string semesterName, bool isUsed, IReadOnlyDictionary<Guid, string> userNames)
    {
        var criteria = config.Criteria
            .OrderBy(c => c.Order)
            .Select(c => new ChecklistCriterionDto(c.Id, c.Order, c.TitleVi, c.TitleEn, c.Description))
            .ToList();

        return new ChecklistConfigDto(
            Id: config.Id,
            SemesterId: config.SemesterId,
            SemesterName: semesterName,
            Version: config.Version,
            Status: config.Status.ToString(),
            PassThreshold: config.PassThreshold,
            CriteriaCount: criteria.Count,
            IsUsed: isUsed,
            CreatedAt: config.CreatedAt,
            CreatedBy: config.CreatedBy,
            CreatedByName: LookupName(userNames, config.CreatedBy),
            UpdatedAt: config.UpdatedAt,
            UpdatedBy: config.UpdatedBy,
            UpdatedByName: LookupName(userNames, config.UpdatedBy),
            Criteria: criteria);
    }

    private static string Lookup(IReadOnlyDictionary<int, string> names, int id)
        => names.TryGetValue(id, out var name) ? name : $"#{id}";

    private static string? LookupName(IReadOnlyDictionary<Guid, string> names, Guid? id)
        => id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;
}
