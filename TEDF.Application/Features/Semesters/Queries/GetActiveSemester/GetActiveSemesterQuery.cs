using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetActiveSemester;

/// <summary>
/// Query to retrieve the currently active semester.
/// </summary>
public record GetActiveSemesterQuery() : IQuery<SemesterDto?>;
