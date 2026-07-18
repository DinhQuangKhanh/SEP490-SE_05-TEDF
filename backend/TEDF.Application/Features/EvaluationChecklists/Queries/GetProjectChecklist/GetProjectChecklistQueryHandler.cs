using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectChecklist;

public class GetProjectChecklistQueryHandler : IQueryHandler<GetProjectChecklistQuery, ProjectChecklistDto>
{
    private readonly IChecklistQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetProjectChecklistQueryHandler(IChecklistQueryService queryService, ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<ProjectChecklistDto> Handle(GetProjectChecklistQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetProjectChecklistAsync(request.ProjectId, _currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");
    }
}
