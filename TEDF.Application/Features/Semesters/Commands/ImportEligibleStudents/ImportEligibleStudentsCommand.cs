using System.IO;
using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;

public record ImportEligibleStudentsCommand(
    int SemesterId,
    Stream FileStream,
    string FileName,
    Guid ImportedBy) : ICommand<ImportEligibleStudentsResponse>;

public record ImportEligibleStudentsResponse(
    int TotalProcessed,
    int SuccessfullyImported,
    List<string> FailedStudentCodes);
