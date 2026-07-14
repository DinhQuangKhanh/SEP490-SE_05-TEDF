using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

public class SaveProjectChecklistCommandHandler : ICommandHandler<SaveProjectChecklistCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IProjectEvaluationChecklistRepository _checklistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveProjectChecklistCommandHandler(
        ICurrentUserService currentUser,
        IProjectRepository projectRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IChecklistConfigRepository configRepository,
        IProjectEvaluationChecklistRepository checklistRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projectRepository = projectRepository;
        _assignmentRepository = assignmentRepository;
        _configRepository = configRepository;
        _checklistRepository = checklistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(SaveProjectChecklistCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var evaluatorId = _currentUser.UserId.Value;

        // Only an actively assigned evaluator may save a checklist for this project.
        _ = await _assignmentRepository.GetActiveByProjectAndEvaluatorAsync(request.ProjectId, evaluatorId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // Checklist can only be edited while the project is awaiting evaluation.
        if (project.Status != ProjectStatus.PendingEvaluation)
            throw new BusinessRuleValidationException(
                "Chỉ có thể cập nhật checklist khi đề tài đang ở trạng thái chờ thẩm định.");

        var submissionNumber = project.EvaluationCount;

        var existing = await _checklistRepository.GetByProjectEvaluatorAsync(
            request.ProjectId, evaluatorId, submissionNumber, cancellationToken);

        if (existing is not null)
        {
            // Continue editing the snapshot the evaluator already started (its criteria match the UI).
            existing.ApplyPassedCriteria(request.PassedCriterionIds, request.Note);
            _checklistRepository.Update(existing);
        }
        else
        {
            var config = await _configRepository.GetActiveBySemesterAsync(project.SemesterId, cancellationToken)
                ?? throw new BusinessRuleValidationException(
                    "Học kỳ này chưa được cấu hình checklist thẩm định. Vui lòng liên hệ Trưởng bộ môn.");

            var checklist = ProjectEvaluationChecklist.CreateFromConfig(config, request.ProjectId, evaluatorId, submissionNumber);
            checklist.ApplyPassedCriteria(request.PassedCriterionIds, request.Note);
            await _checklistRepository.AddAsync(checklist, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
