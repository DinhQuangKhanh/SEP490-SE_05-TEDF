using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Departments.DTOs;

namespace TEDF.Application.Features.Departments.Queries.GetDepartmentProjects;

public record GetDepartmentProjectsQuery : IQuery<DepartmentProjectsResponse>;
