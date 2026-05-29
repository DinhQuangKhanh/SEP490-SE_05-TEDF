using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts SemesterCode to/from string for database storage.
    /// </summary>
    public class SemesterCodeConverter : ValueConverter<SemesterCode, string>
    {
        public SemesterCodeConverter()
            : base(
                code => code.Value,
                value => SemesterCode.Create(value))
        { }
    }
}
