using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ImportChecklistConfig;

/// <summary>
/// Department Head creates a new Draft checklist configuration by importing an Excel file of criteria.
/// The domain parses + validates the file and reports data errors as 400 (never 500).
/// </summary>
[ActionLog("Import Checklist Config", "EvaluationChecklist")]
public record ImportChecklistConfigCommand(
    int SemesterId,
    byte[] FileContent,
    string FileName,
    int RequiredPassCount
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["checklist-configs:"];
}
