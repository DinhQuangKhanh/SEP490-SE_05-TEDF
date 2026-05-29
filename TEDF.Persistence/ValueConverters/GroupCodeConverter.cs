using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts GroupCode to/from string for database storage.
    /// </summary>
    public class GroupCodeConverter : ValueConverter<GroupCode, string>
    {
        public GroupCodeConverter()
            : base(
                code => code.Value,
                value => GroupCode.Create(value))
        { }
    }
}
