using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CopyChecklistConfig;

public class CopyChecklistConfigCommandHandler : ICommandHandler<CopyChecklistConfigCommand, Guid>
{
    private readonly IChecklistDomainService _checklist;

    public CopyChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public Task<Guid> Handle(CopyChecklistConfigCommand request, CancellationToken cancellationToken)
        => _checklist.CopyConfigAsync(request.SourceConfigId, request.TargetSemesterId, cancellationToken);
}
