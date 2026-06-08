using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentProjects;

public record GetDepartmentProjectsQuery : IQuery<DepartmentProjectsResponse>;
