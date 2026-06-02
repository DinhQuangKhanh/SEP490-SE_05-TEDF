using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts ProjectName to/from string for database storage.
    /// </summary>
    public class ProjectNameConverter : ValueConverter<ProjectName, string>
    {
        public ProjectNameConverter()
            : base(
                name => name.Value,
                value => ProjectName.Create(value))
        { }
    }
}
