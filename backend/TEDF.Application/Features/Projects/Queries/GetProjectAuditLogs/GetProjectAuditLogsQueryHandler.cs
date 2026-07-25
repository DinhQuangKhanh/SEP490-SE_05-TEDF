using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetProjectAuditLogs;

public class GetProjectAuditLogsQueryHandler : IQueryHandler<GetProjectAuditLogsQuery, GetProjectAuditLogsResponse>
{
    private readonly IProjectsQueryService _queryService;

    public GetProjectAuditLogsQueryHandler(IProjectsQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<GetProjectAuditLogsResponse> Handle(GetProjectAuditLogsQuery request, CancellationToken cancellationToken)
    {
        return _queryService.GetProjectAuditLogsAsync(request.ProjectId, cancellationToken);
    }
}
