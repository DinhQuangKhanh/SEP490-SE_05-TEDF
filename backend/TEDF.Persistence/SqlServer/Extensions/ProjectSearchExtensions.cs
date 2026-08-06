using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.ProjectAggregate;

namespace TEDF.Persistence.SqlServer.Extensions
{
    /// <summary>
    /// Free-text search over a project's code and names.
    /// </summary>
    /// <remarks>
    /// <see cref="Project.Code"/>, <see cref="Project.NameVi"/> and <see cref="Project.NameEn"/> are
    /// value objects mapped through a <c>ValueConverter</c>, so EF Core cannot translate member access
    /// on them — a predicate such as <c>p.Code.Value.Contains(term)</c> throws at translation time
    /// instead of producing SQL. The searchable columns are therefore materialised for the candidate
    /// set the caller has already narrowed, matched in memory, and the caller re-filters by primary key.
    /// </remarks>
    public static class ProjectSearchExtensions
    {
        /// <summary>
        /// Returns the ids of the candidate projects whose code, Vietnamese/English name or
        /// abbreviation contains <paramref name="search"/> (case-insensitive).
        /// </summary>
        public static async Task<List<Guid>> MatchSearchTermAsync(
            this IQueryable<Project> candidates, string search, CancellationToken cancellationToken = default)
        {
            var term = search.Trim();

            var rows = await candidates
                .Select(p => new { p.Id, p.Code, p.NameVi, p.NameEn, p.NameAbbr })
                .ToListAsync(cancellationToken);

            return rows
                .Where(p => Matches(p.Code?.Value, term)
                            || Matches(p.NameVi?.Value, term)
                            || Matches(p.NameEn?.Value, term)
                            || Matches(p.NameAbbr, term))
                .Select(p => p.Id)
                .ToList();
        }

        private static bool Matches(string? value, string term)
            => value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
