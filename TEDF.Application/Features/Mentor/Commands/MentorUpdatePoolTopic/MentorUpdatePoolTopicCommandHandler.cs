using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;

namespace TEDF.Application.Features.Mentor.Commands.MentorUpdatePoolTopic;

public sealed class MentorUpdatePoolTopicCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MentorUpdatePoolTopicCommand>
{
    public async Task<Unit> Handle(MentorUpdatePoolTopicCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // Only pool-based topics can be updated by mentor through this endpoint
        if (project.SourceType != ProjectSourceType.FromPool)
            throw new BusinessRuleValidationException(
                "Chỉ đề tài trong kho mới có thể được giảng viên chỉnh sửa qua chức năng này.");

        // Only allow update if status is Draft or NeedsModification
        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.NeedsModification)
            throw new BusinessRuleValidationException(
                "Đề tài chỉ có thể chỉnh sửa khi ở trạng thái Nháp hoặc Yêu cầu chỉnh sửa.");

        project.UpdateBasicInfo(
            nameVi: ProjectName.Create(request.NameVi),
            nameEn: ProjectName.Create(request.NameEn),
            nameAbbr: request.NameAbbr,
            description: request.Description,
            objectives: request.Objectives,
            scope: request.Scope,
            technologies: request.Technologies,
            expectedResults: request.ExpectedResults
        );

        project.SetMaxStudents(request.MaxStudents);

        projectRepository.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
