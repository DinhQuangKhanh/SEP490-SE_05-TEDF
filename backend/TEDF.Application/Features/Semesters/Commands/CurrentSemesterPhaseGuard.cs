using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Application.Features.Semesters.Commands;

/// <summary>
/// Shared guard used by Create/Update semester handlers: Registration &amp; Evaluation phases of the
/// new/edited semester must fall within the currently-active semester (topics are registered and
/// vetted during the current semester for the upcoming one).
/// </summary>
public static class CurrentSemesterPhaseGuard
{
    /// <summary>
    /// No-op when there is no active semester (e.g. the first-ever semester) — the Semester
    /// aggregate still guarantees Registration/Evaluation end before the new semester starts.
    /// </summary>
    public static void EnsureWithinCurrentSemester(
        Semester? current,
        SemesterPhaseType type,
        string phaseName,
        DateTime startDate,
        DateTime endDate)
    {
        if (current is null) return;
        if (type is not (SemesterPhaseType.Registration or SemesterPhaseType.Evaluation)) return;

        if (startDate < current.StartDate || endDate > current.EndDate)
            throw new BusinessRuleValidationException(
                $"Giai đoạn {phaseName} phải nằm trong kỳ học hiện tại ({current.Name}).");
    }
}
