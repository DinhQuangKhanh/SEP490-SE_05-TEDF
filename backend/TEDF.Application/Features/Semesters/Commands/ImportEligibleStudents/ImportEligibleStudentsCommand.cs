using System.IO;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;

public record ImportEligibleStudentsCommand(
    int SemesterId,
    Stream FileStream,
    string FileName,
    Guid ImportedBy) : ICommand<ImportEligibleStudentsResponse>;

public record ImportEligibleStudentsResponse(
    int TotalProcessed,
    int SuccessfullyImported,
    List<ImportRowIssue> Issues);
