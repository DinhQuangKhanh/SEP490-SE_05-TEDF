using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts TimeOnly to/from TimeSpan for SQL Server storage.
    /// </summary>
    public class TimeOnlyConverter : ValueConverter<TimeOnly, TimeSpan>
    {
        public TimeOnlyConverter()
            : base(
                timeOnly => timeOnly.ToTimeSpan(),
                timeSpan => TimeOnly.FromTimeSpan(timeSpan))
        { }
    }
}
