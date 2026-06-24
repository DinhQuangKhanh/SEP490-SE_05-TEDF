using System.IO;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleMentors;

public record ImportEligibleMentorsCommand(
    int SemesterId,
    Stream FileStream,
    string FileName,
    Guid ImportedBy) : ICommand<ImportEligibleMentorsResponse>;

public record ImportEligibleMentorsResponse(
    int TotalProcessed,
    int SuccessfullyImported,
    List<ImportRowIssue> Issues);
