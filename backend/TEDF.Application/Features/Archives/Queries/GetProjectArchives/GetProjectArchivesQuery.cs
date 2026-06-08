using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Archives.DTOs;

namespace TEDF.Application.Features.Archives.Queries.GetProjectArchives;

/// <summary>Admin-only: archived projects grouped by academic year, with counts and total size.</summary>
public record GetProjectArchivesQuery() : IQuery<List<ArchiveGroupDto>>;
