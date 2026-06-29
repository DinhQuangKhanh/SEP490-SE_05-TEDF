using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.RemoveEligibleMentors;

public class RemoveEligibleMentorsCommandHandler : ICommandHandler<RemoveEligibleMentorsCommand>
{
    private readonly ISemestersDomainService _semesters;

    public RemoveEligibleMentorsCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(RemoveEligibleMentorsCommand request, CancellationToken cancellationToken)
    {
        await _semesters.RemoveEligibleMentorsAsync(request.SemesterId, request.MentorIds, cancellationToken);
        return Unit.Value;
    }
}
