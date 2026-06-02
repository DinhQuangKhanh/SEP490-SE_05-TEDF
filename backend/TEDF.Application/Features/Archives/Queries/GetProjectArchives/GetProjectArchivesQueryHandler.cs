using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Archives.DTOs;
using TEDF.Domain.Entities;

namespace TEDF.Application.Features.Archives.Queries.GetProjectArchives;

public class GetProjectArchivesQueryHandler : IQueryHandler<GetProjectArchivesQuery, List<ArchiveGroupDto>>
{
    private readonly IProjectArchiveRepository _repository;

    public GetProjectArchivesQueryHandler(IProjectArchiveRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ArchiveGroupDto>> Handle(GetProjectArchivesQuery request, CancellationToken cancellationToken)
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
