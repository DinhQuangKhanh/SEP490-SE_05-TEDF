using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.DirectTopics.Commands.CreateDirectTopic;

public class CreateDirectTopicCommandHandler : ICommandHandler<CreateDirectTopicCommand, Guid>
{
    private readonly IDirectTopicsDomainService _directTopics;
    private readonly ICurrentUserService _currentUser;

    public CreateDirectTopicCommandHandler(IDirectTopicsDomainService directTopics, ICurrentUserService currentUser)
    {
        _directTopics = directTopics;
        _currentUser = currentUser;
    }

    public Task<Guid> Handle(CreateDirectTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return _directTopics.CreateDirectTopicAsync(
            userId, request.GroupId, request.MentorId, request.MajorId,
            new DirectTopicContent(
                request.NameVi, request.NameEn, request.NameAbbr, request.Description,
                request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents),
            cancellationToken);
    }
}
