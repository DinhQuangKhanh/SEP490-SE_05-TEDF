using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetMySupervisedProjects;

/// <summary>Query to retrieve the projects the authenticated mentor actively supervises.</summary>
public record GetMySupervisedProjectsQuery() : IQuery<GetMySupervisedProjectsResult>;
