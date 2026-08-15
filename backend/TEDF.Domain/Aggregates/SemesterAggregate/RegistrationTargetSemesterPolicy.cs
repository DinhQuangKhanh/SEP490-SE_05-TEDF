using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Aggregates.SemesterAggregate
{
    /// <summary>
    /// Decides which semester a topic proposed *right now* belongs to.
    /// <para>
    /// Registration and Evaluation phases of semester N deliberately sit <b>before</b> semester N
    /// starts (see <see cref="Semester.AddPhase"/>): topics are proposed and vetted during the
    /// running semester for the upcoming one. So the semester a new topic belongs to is never the
    /// one the clock is currently inside — asking for "the active semester" is what stamped Fall
    /// 2026 topics with Summer 2026.
    /// </para>
    /// <para>
    /// Kept as a pure function over a candidate list so the rule can be exercised without a
    /// database, and so the Domain keeps its zero-dependency guarantee.
    /// </para>
    /// </summary>
    public static class RegistrationTargetSemesterPolicy
    {
        /// <summary>
        /// The semester a topic proposed at <paramref name="nowUtc"/> will be carried out in:
        /// <list type="number">
        /// <item>the semester whose Registration or Evaluation phase contains <paramref name="nowUtc"/>;</item>
        /// <item>failing that, the earliest semester that has not started yet.</item>
        /// </list>
        /// Null when neither exists — the caller should tell the admin to create the next semester
        /// rather than fall back to the running one.
        /// </summary>
        /// <param name="candidates">
        /// Semesters to consider, each with its <see cref="Semester.Phases"/> loaded. Semesters that
        /// already ended are ignored, so the caller may pass the whole set.
        /// </param>
        /// <param name="nowUtc">The moment the topic is being proposed, in UTC.</param>
        public static Semester? Resolve(IEnumerable<Semester> candidates, DateTime nowUtc)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            // Ordering is applied before every FirstOrDefault so the answer never depends on the
            // order rows happen to come back in — overlapping semesters are guarded on write, but
            // a bad import must still resolve deterministically.
            var open = candidates
                .Where(s => s.EndDate >= nowUtc)
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.Id)
                .ToList();

            var byPhase = open.FirstOrDefault(s => s.Phases.Any(p =>
                IsProposalPhase(p.Type) && p.StartDate <= nowUtc && p.EndDate >= nowUtc));

            return byPhase ?? open.FirstOrDefault(s => s.StartDate > nowUtc);
        }

        private static bool IsProposalPhase(SemesterPhaseType type)
            => type is SemesterPhaseType.Registration or SemesterPhaseType.Evaluation;
    }
}
