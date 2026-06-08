using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Semesters.Commands.DeleteSemester;

/// <summary>Deletes an upcoming semester by delegating to <see cref="ISemestersDomainService"/>.</summary>
public class DeleteSemesterCommandHandler : ICommandHandler<DeleteSemesterCommand>
{
    private readonly ISemestersDomainService _semesters;

    public DeleteSemesterCommandHandler(ISemestersDomainService semesters) => _semesters = semesters;

    public async Task<Unit> Handle(DeleteSemesterCommand request, CancellationToken cancellationToken)
    {
        await _semesters.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
