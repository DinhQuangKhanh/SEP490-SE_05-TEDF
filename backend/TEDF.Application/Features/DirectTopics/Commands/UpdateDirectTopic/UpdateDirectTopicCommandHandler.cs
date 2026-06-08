using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.DirectTopics.Commands.UpdateDirectTopic;

public sealed class UpdateDirectTopicCommandHandler(IDirectTopicsDomainService directTopics)
    : ICommandHandler<UpdateDirectTopicCommand>
{
    public async Task<Unit> Handle(UpdateDirectTopicCommand request, CancellationToken cancellationToken)
    {
        await directTopics.UpdateDirectTopicAsync(
            request.ProjectId,
            new DirectTopicContent(
                request.NameVi, request.NameEn, request.NameAbbr, request.Description,
                request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents),
            cancellationToken);
        return Unit.Value;
    }
}
