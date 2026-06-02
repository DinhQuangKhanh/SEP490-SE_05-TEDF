namespace TEDF.Domain.Enums.Group;

/// <summary>
/// Status of a group invitation.
/// </summary>
public enum GroupInvitationStatus
{
    /// <summary>Invitation is pending a response.</summary>
    Pending = 0,

    /// <summary>Invitation was accepted.</summary>
    Accepted = 1,

    /// <summary>Invitation was rejected.</summary>
    Rejected = 2,

    /// <summary>Invitation has expired.</summary>
    Expired = 3
}
