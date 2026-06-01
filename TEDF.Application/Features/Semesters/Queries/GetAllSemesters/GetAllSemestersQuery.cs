using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetAllSemesters;

/// <summary>
/// Query to retrieve all semesters with their phases, optionally filtered by status.
/// </summary>
public record GetAllSemestersQuery(string? Status = null) : IQuery<List<SemesterDto>>;
