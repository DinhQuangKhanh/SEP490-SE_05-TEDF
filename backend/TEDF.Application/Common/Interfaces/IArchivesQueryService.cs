using TEDF.Application.Features.Archives.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Archives feature. Query handlers depend on this only.
/// </summary>
public interface IArchivesQueryService
{
    Task<List<ArchiveGroupDto>> GetArchivesAsync(CancellationToken cancellationToken = default);
}
