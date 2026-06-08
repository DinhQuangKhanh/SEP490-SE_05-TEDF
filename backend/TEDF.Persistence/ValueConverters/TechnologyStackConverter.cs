using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts TechnologyStack to/from string for database storage.
    /// </summary>
    public class TechnologyStackConverter : ValueConverter<TechnologyStack?, string?>
    {
        public TechnologyStackConverter()
            : base(
                stack => stack == null ? null : stack.Value,
                value => string.IsNullOrEmpty(value) ? null : TechnologyStack.Create(value))
        { }
    }

}
