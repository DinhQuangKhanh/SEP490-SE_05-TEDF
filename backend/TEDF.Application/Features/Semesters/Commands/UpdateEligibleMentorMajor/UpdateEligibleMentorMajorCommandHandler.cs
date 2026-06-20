using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.UpdateEligibleMentorMajor;

public class UpdateEligibleMentorMajorCommandHandler : ICommandHandler<UpdateEligibleMentorMajorCommand>
{
    private readonly ISemestersDomainService _semesters;

    public UpdateEligibleMentorMajorCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(UpdateEligibleMentorMajorCommand request, CancellationToken cancellationToken)
    {
        await _semesters.UpdateEligibleMentorMajorAsync(
            request.SemesterId, request.MentorId, request.MajorId, cancellationToken);
        return Unit.Value;
    }
}
