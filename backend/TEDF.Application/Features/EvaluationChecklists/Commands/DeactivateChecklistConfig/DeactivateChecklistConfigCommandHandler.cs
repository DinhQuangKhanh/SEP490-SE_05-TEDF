using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.DeactivateChecklistConfig;

public class DeactivateChecklistConfigCommandHandler : ICommandHandler<DeactivateChecklistConfigCommand>
{
    private readonly IChecklistDomainService _checklist;

    public DeactivateChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public async Task<Unit> Handle(DeactivateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        await _checklist.DeactivateConfigAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
