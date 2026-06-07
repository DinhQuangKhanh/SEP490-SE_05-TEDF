using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetStudentGroup;

public record GetStudentGroupQuery(int? SemesterId) : IQuery<StudentGroupDto?>;
