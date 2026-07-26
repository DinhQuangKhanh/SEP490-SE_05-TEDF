using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetProjectAuditLogs;

public record GetProjectAuditLogsQuery(Guid ProjectId) : IQuery<GetProjectAuditLogsResponse>;
