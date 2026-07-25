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
        var scores = request.Items
            .Select(i => new ChecklistScoreData(i.CriterionId, i.Score, i.Comment))
            .ToList();

        await _checklist.SaveProjectChecklistAsync(request.ProjectId, scores, request.Note, cancellationToken);
        return Unit.Value;
    }
}
