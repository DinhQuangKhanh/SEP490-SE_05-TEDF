using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;

/// <summary>Imports eligible students by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class ImportEligibleStudentsCommandHandler : ICommandHandler<ImportEligibleStudentsCommand, ImportEligibleStudentsResponse>
{
    private readonly ISemestersDomainService _semesters;

    public ImportEligibleStudentsCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<ImportEligibleStudentsResponse> Handle(ImportEligibleStudentsCommand request, CancellationToken cancellationToken)
    {
        var result = await _semesters.ImportEligibleStudentsAsync(
            request.SemesterId, request.FileStream, request.FileName, request.ImportedBy, cancellationToken);

        return new ImportEligibleStudentsResponse(
            result.TotalProcessed, result.SuccessfullyImported, result.Issues.ToList());
    }
}
