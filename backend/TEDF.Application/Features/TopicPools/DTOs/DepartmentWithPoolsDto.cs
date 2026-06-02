namespace TEDF.Application.Features.TopicPools.DTOs;

/// <summary>
/// Represents a faculty/department with its majors and their corresponding topic pools.
/// Used for the hierarchical "kho đề tài theo khoa" view.
/// </summary>
public class DepartmentWithPoolsDto
{
    public int DepartmentId { get; init; }
    public string DepartmentCode { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public List<MajorWithPoolDto> Majors { get; init; } = [];
}

/// <summary>
/// Represents a major (chuyên ngành) with its associated topic pool.
/// </summary>
public class MajorWithPoolDto
{
    public int MajorId { get; init; }
    public string MajorCode { get; init; } = string.Empty;
    public string MajorName { get; init; } = string.Empty;

    /// <summary>
    /// The topic pool for this major. Null if the major has no pool yet.
    /// </summary>
    public TopicPoolSummaryDto? Pool { get; init; }
}

/// <summary>
/// Compact pool info for the department overview.
/// </summary>
public class TopicPoolSummaryDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public int TotalTopics { get; init; }
}
