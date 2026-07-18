using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the topic-evaluation checklist. See <see cref="IChecklistDomainService"/>.
/// Owns all business rules, authorization and persistence for the checklist command flows.
/// </summary>
public class ChecklistDomainService : IChecklistDomainService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IProjectEvaluationChecklistRepository _checklistRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChecklistDomainService(
        ICurrentUserService currentUser,
        IProjectRepository projectRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IChecklistConfigRepository configRepository,
        IProjectEvaluationChecklistRepository checklistRepository,
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projectRepository = projectRepository;
        _assignmentRepository = assignmentRepository;
        _configRepository = configRepository;
        _checklistRepository = checklistRepository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task SaveProjectChecklistAsync(
        Guid projectId, IReadOnlyList<Guid> passedCriterionIds, string? note, CancellationToken cancellationToken = default)
    {
        var evaluatorId = RequireUserId();

        // Only an actively assigned evaluator may save a checklist for this project.
        _ = await _assignmentRepository.GetActiveByProjectAndEvaluatorAsync(projectId, evaluatorId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        // Checklist can only be edited while the project is awaiting evaluation.
        if (project.Status != ProjectStatus.PendingEvaluation)
            throw new BusinessRuleValidationException(
                "Chỉ có thể cập nhật checklist khi đề tài đang ở trạng thái chờ thẩm định.");

        var submissionNumber = project.EvaluationCount;

        var existing = await _checklistRepository.GetByProjectEvaluatorAsync(
            projectId, evaluatorId, submissionNumber, cancellationToken);

        if (existing is not null)
        {
            // Continue editing the snapshot the evaluator already started (its criteria match the UI).
            existing.ApplyPassedCriteria(passedCriterionIds, note);
            _checklistRepository.Update(existing);
        }
        else
        {
            var config = await _configRepository.GetActiveBySemesterAsync(project.SemesterId, cancellationToken)
                ?? throw new BusinessRuleValidationException(
                    "Học kỳ này chưa được cấu hình checklist thẩm định. Vui lòng liên hệ Trưởng bộ môn.");

            var checklist = ProjectEvaluationChecklist.CreateFromConfig(config, projectId, evaluatorId, submissionNumber);
            checklist.ApplyPassedCriteria(passedCriterionIds, note);
            await _checklistRepository.AddAsync(checklist, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateConfigAsync(
        int semesterId, IReadOnlyList<ChecklistCriterionData> criteria, CancellationToken cancellationToken = default)
    {
        _ = await _semesterRepository.GetByIdAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        var version = await _configRepository.GetMaxVersionForSemesterAsync(semesterId, cancellationToken) + 1;

        var config = ChecklistConfig.Create(semesterId, version, ToTuples(criteria), createdBy: _currentUser.UserId);

        await _configRepository.AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.Id;
    }

    public async Task<Guid> CopyConfigAsync(
        Guid sourceConfigId, int targetSemesterId, CancellationToken cancellationToken = default)
    {
        var source = await _configRepository.GetByIdAsync(sourceConfigId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), sourceConfigId);

        _ = await _semesterRepository.GetByIdAsync(targetSemesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), targetSemesterId);

        var version = await _configRepository.GetMaxVersionForSemesterAsync(targetSemesterId, cancellationToken) + 1;

        var copy = source.CopyTo(targetSemesterId, version, _currentUser.UserId);

        await _configRepository.AddAsync(copy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return copy.Id;
    }

    public async Task UpdateConfigAsync(
        Guid id, IReadOnlyList<ChecklistCriterionData> criteria, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), id);

        // Domain enforces "Draft only" — an Active/used config must be copied to a new version instead.
        config.ReplaceCriteria(ToTuples(criteria), _currentUser.UserId);

        _configRepository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), id);

        // Retire the current Active config for this semester so only one stays Active.
        var currentActive = await _configRepository.GetActiveBySemesterAsync(config.SemesterId, cancellationToken);
        if (currentActive is not null && currentActive.Id != config.Id)
        {
            currentActive.Deactivate(_currentUser.UserId);
            _configRepository.Update(currentActive);
        }

        // Domain enforces the exactly-10-criteria rule here.
        config.Activate(_currentUser.UserId);
        _configRepository.Update(config);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await _configRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), id);

        config.Deactivate(_currentUser.UserId);
        _configRepository.Update(config);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Guid RequireUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");
        return _currentUser.UserId.Value;
    }

    private static IReadOnlyList<(string TitleVi, string TitleEn, string? Description)> ToTuples(
        IReadOnlyList<ChecklistCriterionData> criteria)
        => criteria.Select(c => (c.TitleVi, c.TitleEn, c.Description)).ToList();
}
