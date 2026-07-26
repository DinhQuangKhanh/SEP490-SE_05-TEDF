using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ImportChecklistConfig;

public class ImportChecklistConfigCommandHandler : ICommandHandler<ImportChecklistConfigCommand, Guid>
{
    private readonly IChecklistDomainService _checklist;

    public ImportChecklistConfigCommandHandler(IChecklistDomainService checklist)
    {
        _checklist = checklist;
    }

    public Task<Guid> Handle(ImportChecklistConfigCommand request, CancellationToken cancellationToken)
        => _checklist.ImportConfigAsync(
            request.SemesterId, request.FileContent, request.FileName, request.RequiredPassCount, cancellationToken);
}
