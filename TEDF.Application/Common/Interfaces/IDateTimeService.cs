namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Provides access to current date and time.
/// This is an Application layer concern as it abstracts system time for testing.
/// </summary>
public interface IDateTimeService
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current date (without time component).
    /// </summary>
    DateOnly Today { get; }
}
