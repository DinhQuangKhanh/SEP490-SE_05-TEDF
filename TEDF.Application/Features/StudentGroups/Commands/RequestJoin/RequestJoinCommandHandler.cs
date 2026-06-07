using TEDF.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RequestJoin;

public class RequestJoinCommandHandler : ICommandHandler<RequestJoinCommand, int>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RequestJoinCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(RequestJoinCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var group = await _groupRepository.GetWithJoinRequestsAsync(request.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), request.GroupId);

        // Check if student is already in an active group this semester
        if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, group.SemesterId, cancellationToken))
            throw new BusinessRuleValidationException("Bạn đã có nhóm hoạt động trong học kỳ này.");

        // A student can only keep one pending join request at a time in the same semester.
        if (await _groupRepository.HasPendingJoinRequestAsync(studentId, group.SemesterId, cancellationToken))
            throw new BusinessRuleValidationException("Bạn đã có một yêu cầu tham gia nhóm đang chờ xử lý trong học kỳ này.");

        // Domain logic validates group status, open for requests, capacity, duplicates
        var joinRequest = group.RequestToJoin(studentId, request.Message);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPendingJoinRequestUniqueViolation(ex))
        {
            throw new BusinessRuleValidationException("Bạn đã có một yêu cầu tham gia nhóm đang chờ xử lý.");
        }

        return joinRequest.Id;
    }

    private static bool IsPendingJoinRequestUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_GroupJoinRequests_GroupId_StudentId_Pending", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("IX_GroupJoinRequests_GroupId_StudentId_Pending", StringComparison.OrdinalIgnoreCase);
    }
}
