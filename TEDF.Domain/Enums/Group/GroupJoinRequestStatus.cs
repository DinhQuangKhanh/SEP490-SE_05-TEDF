namespace TEDF.Domain.Enums.Group;

/// <summary>
/// Status of a group join request.
/// </summary>
public enum GroupJoinRequestStatus
{
    /// <summary>Request is pending review by the group leader.</summary>
    Pending = 0,

    /// <summary>Request was approved by the group leader.</summary>
    Approved = 1,

    /// <summary>Request was rejected by the group leader.</summary>
    Rejected = 2
}
