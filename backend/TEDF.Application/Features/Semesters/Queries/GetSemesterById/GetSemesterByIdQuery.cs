using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetSemesterById;

/// <summary>
/// Query to retrieve a single semester by its ID.
/// </summary>
public record GetSemesterByIdQuery(int SemesterId) : IQuery<SemesterDto>;
