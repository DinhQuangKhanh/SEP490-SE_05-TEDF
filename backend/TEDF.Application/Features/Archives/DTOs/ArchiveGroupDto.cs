namespace TEDF.Application.Features.Archives.DTOs;

/// <summary>Archived projects summarised per academic year (for the admin storage table).</summary>
public record ArchiveGroupDto(
    string AcademicYear,
    int ProjectCount,
    long TotalSizeBytes);
