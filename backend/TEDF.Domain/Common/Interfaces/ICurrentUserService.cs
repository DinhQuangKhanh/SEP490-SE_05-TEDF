namespace TEDF.Domain.Common.Interfaces
{
    /// <summary>
    /// DEPRECATED: This interface has been moved to TEDF.Application.Common.Interfaces.
    /// This alias is kept for backward compatibility. Please use the Application layer version.
    /// </summary>
    [Obsolete("Use TEDF.Application.Common.Interfaces.ICurrentUserService instead. This interface will be removed in a future version.")]
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Email { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
}
