using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;

public class CreateChecklistConfigCommandHandler : ICommandHandler<CreateChecklistConfigCommand, Guid>
{
    private readonly IChecklistDomainService _checklist;

    public CreateChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public Task<Guid> Handle(CreateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var criteria = request.Criteria
            .Select(c => new ChecklistCriterionData(c.TitleVi, c.TitleEn, c.Description, c.MaxScore, c.PassScore))
            .ToList();

        return _checklist.CreateConfigAsync(request.SemesterId, criteria, request.RequiredPassCount, cancellationToken);
    }
}
