using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Entities;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Common.Exceptions;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectChecklist;

public class GetProjectChecklistQueryHandler : IQueryHandler<GetProjectChecklistQuery, ProjectChecklistDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IProjectEvaluationChecklistRepository _checklistRepository;

    public GetProjectChecklistQueryHandler(
        ICurrentUserService currentUser,
        IProjectRepository projectRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IChecklistConfigRepository configRepository,
        IProjectEvaluationChecklistRepository checklistRepository)
    {
        _currentUser = currentUser;
        _projectRepository = projectRepository;
        _assignmentRepository = assignmentRepository;
        _configRepository = configRepository;
        _checklistRepository = checklistRepository;
    }

    public async Task<ProjectChecklistDto> Handle(GetProjectChecklistQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var evaluatorId = _currentUser.UserId.Value;

        // Only an actively assigned evaluator may view the checklist for this project.
        _ = await _assignmentRepository.GetActiveByProjectAndEvaluatorAsync(request.ProjectId, evaluatorId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // A previously saved result for the current round takes precedence (returns the exact version used).
        var saved = await _checklistRepository.GetByProjectEvaluatorAsync(
            request.ProjectId, evaluatorId, project.EvaluationCount, cancellationToken);

        if (saved is not null)
        {
            // Enrich the snapshot rows with the English title / description from the exact config version
            // used (falls back to the stored Vietnamese title if that version can no longer be loaded).
            var usedConfig = await _configRepository.GetByIdAsync(saved.ChecklistConfigId, cancellationToken);
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
                ProjectId: request.ProjectId,
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
        var config = await _configRepository.GetActiveBySemesterAsync(project.SemesterId, cancellationToken);
        if (config is null)
        {
            return new ProjectChecklistDto(
                ProjectId: request.ProjectId,
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
            ProjectId: request.ProjectId,
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
}
