using TEDF.Application.Common.Interfaces;

namespace TEDF.Persistence.Services
{
    /// <summary>
    /// Implementation of IDateTimeService providing current date and time.
    /// Useful for testing by allowing time to be mocked.
    /// </summary>
    public class DateTimeService : IDateTimeService
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;

        /// <inheritdoc />
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    }
}
