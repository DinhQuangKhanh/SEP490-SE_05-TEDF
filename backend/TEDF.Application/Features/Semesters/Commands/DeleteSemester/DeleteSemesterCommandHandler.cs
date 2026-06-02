using TEDF.Application.Common.Abstractions;
using MediatR;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Application.Features.Semesters.Commands.DeleteSemester;

/// <summary>
/// Handles the DeleteSemesterCommand to remove an upcoming semester.
/// </summary>
public class DeleteSemesterCommandHandler : ICommandHandler<DeleteSemesterCommand>
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSemesterCommandHandler(
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork)
    {
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteSemesterCommand request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), request.Id);

        if (semester.Status != SemesterStatus.Upcoming)
        {
            throw new BusinessRuleValidationException("Only upcoming semesters can be deleted.");
        }

        _semesterRepository.Remove(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
