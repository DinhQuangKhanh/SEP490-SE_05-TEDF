using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.TopicPoolAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts TopicCode to/from string for database storage.
    /// </summary>
    public class TopicCodeConverter : ValueConverter<TopicCode, string>
    {
        public TopicCodeConverter()
            : base(
                code => code.Value,
                value => TopicCode.Create(value))
        { }
    }
}
