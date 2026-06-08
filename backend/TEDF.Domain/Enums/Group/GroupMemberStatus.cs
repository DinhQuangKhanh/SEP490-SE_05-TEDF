namespace TEDF.Domain.Enums.Group;

/// <summary>
/// Status of a group member.
/// </summary>
public enum GroupMemberStatus
{
    /// <summary>Member is currently active in the group.</summary>
    Active = 0,

    /// <summary>Member has left the group.</summary>
    Left = 1
}