using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;

public class UpdateChecklistConfigCommandHandler : ICommandHandler<UpdateChecklistConfigCommand>
{
    private readonly IChecklistDomainService _checklist;

    public UpdateChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public async Task<Unit> Handle(UpdateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var criteria = request.Criteria
            .Select(c => new ChecklistCriterionData(c.TitleVi, c.TitleEn, c.Description))
            .ToList();

        await _checklist.UpdateConfigAsync(request.Id, criteria, request.RequiredPassCount, cancellationToken);
        return Unit.Value;
    }
}
