using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts SubmissionNumber to/from int for database storage.
    /// </summary>
    public class SubmissionNumberConverter : ValueConverter<SubmissionNumber, int>
    {
        public SubmissionNumberConverter()
            : base(
                number => number.Value,
                value => SubmissionNumber.Create(value))
        { }
    }
}
