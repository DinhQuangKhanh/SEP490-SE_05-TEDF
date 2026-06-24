using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Semesters.Commands.UpdateEligibleMentorMajor;

/// <summary>Corrects the assigned program (Major) of a rostered mentor — the inline admin edit.</summary>
[ActionLog("Update Eligible Mentor Major", "Semester")]
public record UpdateEligibleMentorMajorCommand(int SemesterId, Guid MentorId, int MajorId) : ICommand;
