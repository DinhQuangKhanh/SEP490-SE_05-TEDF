namespace TEDF.Domain.Enums.User;

/// <summary>
/// Status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>User account is active and can access the system.</summary>
    Active = 0,

    /// <summary>User account is temporarily locked.</summary>
    Locked = 1,

    /// <summary>User account is inactive/disabled.</summary>
    Inactive = 2
}