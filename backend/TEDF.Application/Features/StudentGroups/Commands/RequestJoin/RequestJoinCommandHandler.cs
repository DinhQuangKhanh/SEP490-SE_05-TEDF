using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RequestJoin;

public class RequestJoinCommandHandler : ICommandHandler<RequestJoinCommand, int>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public RequestJoinCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public Task<int> Handle(RequestJoinCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        return _groups.RequestJoinAsync(request.GroupId, studentId, request.Message, cancellationToken);
    }
}
