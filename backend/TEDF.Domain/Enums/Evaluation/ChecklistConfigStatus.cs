namespace TEDF.Domain.Enums.Evaluation;

/// <summary>
/// Lifecycle status of a topic-evaluation checklist configuration (per semester).
/// </summary>
public enum ChecklistConfigStatus
{
    /// <summary>Being edited; not yet applied to any evaluation.</summary>
    Draft = 0,

    /// <summary>Currently applied to the semester. At most one Active config per semester.</summary>
    Active = 1,

    /// <summary>Retired; kept for history but no longer applied to new evaluations.</summary>
    Inactive = 2
}
