using TEDF.Application.Common.Abstractions;
using MediatR;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Application.Features.Semesters.Commands.UpdateSemester;

/// <summary>
/// Handles the UpdateSemesterCommand by validating and updating an existing semester.
/// The domain's EnsureUpcoming guard rejects updates for Ongoing/Ended semesters.
/// </summary>
public class UpdateSemesterCommandHandler : ICommandHandler<UpdateSemesterCommand>
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSemesterCommandHandler(
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork)
    {
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), request.Id);

        // Validate no overlapping semesters (exclude current semester)
        if (await _semesterRepository.HasOverlappingAsync(request.StartDate, request.EndDate, request.Id, cancellationToken))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        // Domain guards will throw if semester is not Upcoming
        semester.UpdateDetails(request.Name, request.Description);
        semester.UpdateDates(request.StartDate, request.EndDate);

        // Update phase dates if provided
        if (request.Phases is { Count: > 0 })
        {
            foreach (var phase in request.Phases)
            {
                semester.UpdatePhaseDates(phase.Id, phase.StartDate, phase.EndDate);
            }
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
