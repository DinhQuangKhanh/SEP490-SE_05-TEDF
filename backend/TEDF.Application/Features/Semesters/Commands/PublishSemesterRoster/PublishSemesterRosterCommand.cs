using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Semesters.Commands.PublishSemesterRoster;

/// <summary>
/// Finalizes a semester's eligibility roster: dispatches mentor notifications and the
/// eligible-student email job. Sent once by the admin after imports are complete.
/// </summary>
[ActionLog("Publish Semester Roster", "Semester")]
public record PublishSemesterRosterCommand(int SemesterId, Guid PublishedBy) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["semesters:"];
}
