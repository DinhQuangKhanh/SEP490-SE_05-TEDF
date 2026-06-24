using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.RemoveEligibleStudents;

public class RemoveEligibleStudentsCommandHandler : ICommandHandler<RemoveEligibleStudentsCommand>
{
    private readonly ISemestersDomainService _semesters;

    public RemoveEligibleStudentsCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(RemoveEligibleStudentsCommand request, CancellationToken cancellationToken)
    {
        await _semesters.RemoveEligibleStudentsAsync(request.SemesterId, request.StudentIds, cancellationToken);
        return Unit.Value;
    }
}
