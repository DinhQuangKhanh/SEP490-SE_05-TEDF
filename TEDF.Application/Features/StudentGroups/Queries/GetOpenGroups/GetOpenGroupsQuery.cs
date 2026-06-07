using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetOpenGroups;

public record GetOpenGroupsQuery(int? SemesterId) : IQuery<List<OpenGroupDto>>;
