using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Entities;

/// <summary>
/// Represents a student who is eligible to register for a topic in this semester.
/// Contact details and program (Major) are snapshotted from the imported Excel so the
/// roster can be emailed/notified independently of the User table.
/// </summary>
public class EligibleStudent : Entity<int>
{
    public int SemesterId { get; private set; }
    public Guid StudentId { get; private set; }
    public string StudentCode { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public int? MajorId { get; private set; }
    public bool IsEligible { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public Guid? ImportedBy { get; private set; }

    private EligibleStudent() { }

    internal static EligibleStudent Create(int semesterId, Guid studentId, string studentCode,
        string? email = null, string? phoneNumber = null, int? majorId = null, Guid? importedBy = null)
    {
        return new EligibleStudent
        {
            SemesterId = semesterId,
            StudentId = studentId,
            StudentCode = studentCode,
            Email = email,
            PhoneNumber = phoneNumber,
            MajorId = majorId,
            IsEligible = true,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = importedBy
        };
    }

    /// <summary>Refreshes the snapshotted contact/program columns from a later (supplementary) import.</summary>
    internal void UpdateSnapshot(string? email, string? phoneNumber, int? majorId)
    {
        if (!string.IsNullOrWhiteSpace(email)) Email = email;
        if (!string.IsNullOrWhiteSpace(phoneNumber)) PhoneNumber = phoneNumber;
        if (majorId.HasValue) MajorId = majorId;
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
