using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Archives.DTOs;

namespace TEDF.Application.Features.Archives.Queries.GetProjectArchives;

public class GetProjectArchivesQueryHandler : IQueryHandler<GetProjectArchivesQuery, List<ArchiveGroupDto>>
{
    private readonly IArchivesQueryService _archives;

    public GetProjectArchivesQueryHandler(IArchivesQueryService archives) => _archives = archives;

    public Task<List<ArchiveGroupDto>> Handle(GetProjectArchivesQuery request, CancellationToken cancellationToken)
        => _archives.GetArchivesAsync(cancellationToken);
}
