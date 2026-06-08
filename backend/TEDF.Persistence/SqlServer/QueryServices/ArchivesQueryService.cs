using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Archives.DTOs;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Archives feature. See <see cref="IArchivesQueryService"/>.
/// </summary>
public class ArchivesQueryService : IArchivesQueryService
{
    private readonly IProjectArchiveRepository _repository;

    public ArchivesQueryService(IProjectArchiveRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ArchiveGroupDto>> GetArchivesAsync(CancellationToken cancellationToken = default)
    {
        var archives = await _repository.GetAllAsync(cancellationToken);
        return archives
            .GroupBy(a => a.AcademicYear)
            .Select(g => new ArchiveGroupDto(
                g.Key,
                g.Count(),
                g.Sum(a => a.FileSizeBytes ?? 0)))
            .OrderByDescending(g => g.AcademicYear)
            .ToList();
    }
}
