using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.PublishSemesterRoster;

/// <summary>Publishes the roster by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class PublishSemesterRosterCommandHandler : ICommandHandler<PublishSemesterRosterCommand>
{
    private readonly ISemestersDomainService _semesters;

    public PublishSemesterRosterCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(PublishSemesterRosterCommand request, CancellationToken cancellationToken)
    {
        await _semesters.PublishRosterAsync(request.SemesterId, request.PublishedBy, cancellationToken);
        return Unit.Value;
    }
}
