using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.UpdateSemester;

/// <summary>Updates a semester by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class UpdateSemesterCommandHandler : ICommandHandler<UpdateSemesterCommand>
{
    private readonly ISemestersDomainService _semesters;

    public UpdateSemesterCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(UpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        await _semesters.UpdateAsync(
            request.Id,
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Phases?.Select(p => new SemesterPhaseDateChange(p.Id, p.StartDate, p.EndDate)).ToList(),
            cancellationToken);
        return Unit.Value;
    }
}
