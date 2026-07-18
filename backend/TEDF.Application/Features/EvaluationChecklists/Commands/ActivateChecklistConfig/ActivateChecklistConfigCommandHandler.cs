using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ActivateChecklistConfig;

public class ActivateChecklistConfigCommandHandler : ICommandHandler<ActivateChecklistConfigCommand>
{
    private readonly IChecklistDomainService _checklist;

    public ActivateChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public async Task<Unit> Handle(ActivateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        await _checklist.ActivateConfigAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
