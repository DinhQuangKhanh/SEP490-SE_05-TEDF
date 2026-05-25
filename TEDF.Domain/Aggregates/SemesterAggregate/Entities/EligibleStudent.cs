using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Entities;

/// <summary>
/// Represents a student who is eligible to register for a topic in this semester.
/// </summary>
public class EligibleStudent : Entity<int>
{
    public int SemesterId { get; private set; }
    public Guid StudentId { get; private set; }
    public string StudentCode { get; private set; } = string.Empty;
    public bool IsEligible { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public Guid? ImportedBy { get; private set; }

    private EligibleStudent() { }

    internal static EligibleStudent Create(int semesterId, Guid studentId, string studentCode, Guid? importedBy = null)
    {
        return new EligibleStudent
        {
            SemesterId = semesterId,
            StudentId = studentId,
            StudentCode = studentCode,
            IsEligible = true,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = importedBy
        };
    }

    internal void RevokeEligibility()
    {
        IsEligible = false;
    }

    internal void ReinstateEligibility()
    {
        IsEligible = true;
    }
}
