using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.MentorUpdatePoolTopic;

public sealed class MentorUpdatePoolTopicCommandHandler(ITopicPoolsDomainService topicPools)
    : ICommandHandler<MentorUpdatePoolTopicCommand>
{
    public async Task<Unit> Handle(MentorUpdatePoolTopicCommand request, CancellationToken cancellationToken)
    {
        await topicPools.UpdatePoolTopicAsync(
            request.ProjectId,
            new PoolTopicContent(
                request.NameVi, request.NameEn, request.NameAbbr, request.Description,
                request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents),
            cancellationToken);
        return Unit.Value;
    }
}
