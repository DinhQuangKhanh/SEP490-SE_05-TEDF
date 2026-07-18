using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

public class SaveProjectChecklistCommandHandler : ICommandHandler<SaveProjectChecklistCommand>
{
    private readonly IChecklistDomainService _checklist;

    public SaveProjectChecklistCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public async Task<Unit> Handle(SaveProjectChecklistCommand request, CancellationToken cancellationToken)
    {
        await _checklist.SaveProjectChecklistAsync(
            request.ProjectId, request.PassedCriterionIds, request.Note, cancellationToken);
        return Unit.Value;
    }
}
