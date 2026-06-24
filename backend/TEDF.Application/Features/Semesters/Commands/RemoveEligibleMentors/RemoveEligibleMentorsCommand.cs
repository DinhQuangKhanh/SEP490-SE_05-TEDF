using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Semesters.Commands.RemoveEligibleMentors;

[ActionLog("Remove Eligible Mentors", "Semester")]
public record RemoveEligibleMentorsCommand(int SemesterId, IReadOnlyList<Guid> MentorIds) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["semesters:"];
}
