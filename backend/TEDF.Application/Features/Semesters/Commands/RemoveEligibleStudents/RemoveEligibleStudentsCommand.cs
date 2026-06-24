using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Semesters.Commands.RemoveEligibleStudents;

[ActionLog("Remove Eligible Students", "Semester")]
public record RemoveEligibleStudentsCommand(int SemesterId, IReadOnlyList<Guid> StudentIds) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["semesters:"];
}
