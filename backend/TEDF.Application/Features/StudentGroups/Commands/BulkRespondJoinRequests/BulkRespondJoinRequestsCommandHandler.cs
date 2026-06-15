using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.BulkRespondJoinRequests;

public class BulkRespondJoinRequestsCommandHandler : ICommandHandler<BulkRespondJoinRequestsCommand, BulkOperationResultDto>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public BulkRespondJoinRequestsCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public async Task<BulkOperationResultDto> Handle(BulkRespondJoinRequestsCommand request, CancellationToken cancellationToken)
    {
        var leaderId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        if (request.RequestIds == null || request.RequestIds.Count == 0)
            throw new ArgumentException("Danh sách yêu cầu không được để trống.");

        int successCount = 0;
        var failures = new List<BulkItemFailureDto>();

        foreach (var requestId in request.RequestIds)
        {
            try
            {
                await _groups.RespondJoinRequestAsync(request.GroupId, requestId, leaderId, request.Approve, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                failures.Add(new BulkItemFailureDto(requestId, ex.Message));
            }
        }

        string actionName = request.Approve ? "Chấp nhận" : "Từ chối";
        string message = failures.Count == 0
            ? $"{actionName} thành công {successCount} yêu cầu."
            : $"{actionName} thành công {successCount} yêu cầu, thất bại {failures.Count} yêu cầu.";

        return new BulkOperationResultDto(request.RequestIds.Count, successCount, failures, message);
    }
}
