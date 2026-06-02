using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Semesters.Commands.UpdateSemester;

/// <summary>
/// Command to update an existing semester's details and phase dates.
/// Only allowed when the semester status is Upcoming.
/// </summary>
[ActionLog("Update Semester", "Semester")]
public record UpdateSemesterCommand(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    List<UpdatePhaseDto>? Phases
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["semesters:", "admin:dashboard", "evaluator:filter-options"];
}

/// <summary>
/// DTO for updating phase dates within a semester.
/// </summary>
public record UpdatePhaseDto(int Id, DateTime StartDate, DateTime EndDate);
