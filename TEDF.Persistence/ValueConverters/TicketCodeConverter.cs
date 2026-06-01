using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;

namespace TEDF.Persistence.ValueConverters
{
    /// <summary>
    /// Converts TicketCode to/from string for database storage.
    /// </summary>
    public class TicketCodeConverter : ValueConverter<TicketCode, string>
    {
        public TicketCodeConverter()
            : base(
                code => code.Value,
                value => TicketCode.Create(value))
        { }
    }
}
