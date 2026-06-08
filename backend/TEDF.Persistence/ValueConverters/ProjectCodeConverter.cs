using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts ProjectCode to/from string for database storage.
    /// </summary>
    public class ProjectCodeConverter : ValueConverter<ProjectCode, string>
    {
        public ProjectCodeConverter()
            : base(
                code => code.Value,
                value => ProjectCode.Create(value))
        { }
    }
}
