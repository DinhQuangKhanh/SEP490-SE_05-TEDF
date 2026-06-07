using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.CreateSemester;

/// <summary>Creates a semester by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class CreateSemesterCommandHandler : ICommandHandler<CreateSemesterCommand, int>
{
    private readonly ISemestersDomainService _semesters;

    public CreateSemesterCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public Task<int> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
        => _semesters.CreateAsync(
            request.Name,
            request.Code,
            request.StartDate,
            request.EndDate,
            request.AcademicYearStart,
            request.Description,
            request.Phases.Select(p => new NewSemesterPhase(p.Name, p.Type, p.StartDate, p.EndDate)).ToList(),
            cancellationToken);
}
