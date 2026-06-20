using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Entities;

/// <summary>
/// Represents a mentor (lecturer) assigned to supervise graduation projects in this semester.
/// Mirrors <see cref="EligibleStudent"/>. The assigned program (<see cref="MajorId"/>) lives here
/// because a User has no Major; it drives the "assigned in &lt;major&gt;" notification on publish.
/// </summary>
public class EligibleMentor : Entity<int>
{
    public int SemesterId { get; private set; }
    public Guid MentorId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    /// <summary>Bộ môn giảng viên đang dạy (SE/CF/AI/IA…), snapshot từ file import. Hiển thị, không dùng cho phân công.</summary>
    public string? Division { get; private set; }
    /// <summary>Ngành hướng dẫn được phân công. Có thể null khi import từ file chưa khớp Major — admin chọn sau.</summary>
    public int? MajorId { get; private set; }
    public bool IsAssigned { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public Guid? ImportedBy { get; private set; }

    private EligibleMentor() { }

    internal static EligibleMentor Create(int semesterId, Guid mentorId, string employeeCode, int? majorId,
        string? email = null, string? phoneNumber = null, string? division = null, Guid? importedBy = null)
    {
        return new EligibleMentor
        {
            SemesterId = semesterId,
            MentorId = mentorId,
            EmployeeCode = employeeCode,
            MajorId = majorId,
            Email = email,
            PhoneNumber = phoneNumber,
            Division = division,
            IsAssigned = true,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = importedBy
        };
    }

    /// <summary>Refreshes the snapshotted contact columns from a later (supplementary) import.</summary>
    internal void UpdateSnapshot(string? email, string? phoneNumber, string? division)
    {
        if (!string.IsNullOrWhiteSpace(email)) Email = email;
        if (!string.IsNullOrWhiteSpace(phoneNumber)) PhoneNumber = phoneNumber;
        if (!string.IsNullOrWhiteSpace(division)) Division = division;
    }

    /// <summary>Corrects the assigned program (backs the inline "Program" edit in the admin roster).</summary>
    internal void ChangeMajor(int majorId)
    {
        MajorId = majorId;
    }

    internal void Revoke()
    {
        IsAssigned = false;
    }

    internal void Reinstate()
    {
        IsAssigned = true;
    }
}
