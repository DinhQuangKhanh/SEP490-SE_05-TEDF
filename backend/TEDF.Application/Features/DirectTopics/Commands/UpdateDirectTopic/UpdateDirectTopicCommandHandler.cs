using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;

namespace TEDF.Application.Features.DirectTopics.Commands.UpdateDirectTopic;

public sealed class UpdateDirectTopicCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork) 
    : ICommandHandler<UpdateDirectTopicCommand>
{
    public async Task<Unit> Handle(UpdateDirectTopicCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // Only allow update if status is Draft or NeedsModification
        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.NeedsModification)
            throw new BusinessRuleValidationException("Topic can only be edited in Draft or NeedsModification status.");

        // Update topic details using existing methods
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
