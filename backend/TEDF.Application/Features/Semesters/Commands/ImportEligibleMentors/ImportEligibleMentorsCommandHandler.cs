using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleMentors;

/// <summary>Imports supervising mentors by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class ImportEligibleMentorsCommandHandler : ICommandHandler<ImportEligibleMentorsCommand, ImportEligibleMentorsResponse>
{
    private readonly ISemestersDomainService _semesters;

    public ImportEligibleMentorsCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<ImportEligibleMentorsResponse> Handle(ImportEligibleMentorsCommand request, CancellationToken cancellationToken)
    {
        var result = await _semesters.ImportEligibleMentorsAsync(
            request.SemesterId, request.FileStream, request.FileName, request.ImportedBy, cancellationToken);

        return new ImportEligibleMentorsResponse(
            result.TotalProcessed, result.SuccessfullyImported, result.Issues.ToList());
    }
}
