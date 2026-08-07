using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Users.Commands.ImportUsers;

/// <summary>
/// Admin bulk-imports users from an Excel/CSV stream (Student/Mentor/Evaluator only). Returns a
/// per-row issue summary; invalidates the users list cache.
/// </summary>
public record ImportUsersCommand(Stream FileStream, string FileName, Guid ImportedBy)
    : ICacheInvalidatingCommand<UserImportResponse>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}

public record UserImportResponse(int TotalProcessed, int SuccessfullyImported, List<ImportRowIssue> Issues);
