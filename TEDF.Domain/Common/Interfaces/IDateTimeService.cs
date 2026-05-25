namespace TEDF.Domain.Common.Interfaces
{
    /// <summary>
    /// DEPRECATED: This interface has been moved to TEDF.Application.Common.Interfaces.
    /// This alias is kept for backward compatibility. Please use the Application layer version.
    /// </summary>
    [Obsolete("Use TEDF.Application.Common.Interfaces.IDateTimeService instead. This interface will be removed in a future version.")]
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
        DateOnly Today { get; }
    }
}
