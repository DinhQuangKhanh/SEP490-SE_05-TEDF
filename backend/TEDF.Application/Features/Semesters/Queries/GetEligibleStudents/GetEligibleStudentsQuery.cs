using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetEligibleStudents;

/// <summary>Returns the eligible-student roster for a semester (admin preview table).</summary>
public record GetEligibleStudentsQuery(int SemesterId) : IQuery<List<EligibleStudentDto>>;
