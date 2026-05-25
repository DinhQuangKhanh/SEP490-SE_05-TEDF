using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects
{
    public sealed class ProjectSnapshot : ValueObject
    {
        public string NameVi { get; }
        public string NameEn { get; }
        public string NameAbbr { get; }
        public string Description { get; }
        public string Objectives { get; }
        public string? Scope { get; }
        public string? Technologies { get; }
        public string? ExpectedResults { get; }
        public DateTime CapturedAt { get; }

        private ProjectSnapshot(string nameVi, string nameEn, string nameAbbr, string description, string objectives,
            string? scope, string? technologies, string? expectedResults, DateTime capturedAt)
        {
            NameVi = nameVi;
            NameEn = nameEn;
            NameAbbr = nameAbbr;
            Description = description;
            Objectives = objectives;
            Scope = scope;
            Technologies = technologies;
            ExpectedResults = expectedResults;
            CapturedAt = capturedAt;
        }

        public static ProjectSnapshot Capture(string nameVi, string nameEn, string nameAbbr, string description, string objectives,
            string? scope, string? technologies, string? expectedResults, DateTime capturedAt)
        {
            return new ProjectSnapshot(nameVi, nameEn, nameAbbr, description, objectives, scope, technologies, expectedResults, capturedAt);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return NameVi;
            yield return NameEn;
            yield return NameAbbr;
            yield return Description;
            yield return Objectives;
            yield return Scope;
            yield return Technologies;
            yield return ExpectedResults;
            yield return CapturedAt;
        }
    }
}
