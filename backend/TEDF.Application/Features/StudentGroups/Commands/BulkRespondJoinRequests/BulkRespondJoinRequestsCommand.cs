using System.Collections.Generic;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Commands.BulkRespondJoinRequests;

[ActionLog("Bulk Respond Join Requests", "StudentGroup")]
public record BulkRespondJoinRequestsCommand(Guid GroupId, List<int> RequestIds, bool Approve) : ICacheInvalidatingCommand<BulkOperationResultDto>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["student-groups:"];
}
