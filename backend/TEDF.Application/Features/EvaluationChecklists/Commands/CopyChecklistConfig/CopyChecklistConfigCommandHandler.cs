using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CopyChecklistConfig;

public class CopyChecklistConfigCommandHandler : ICommandHandler<CopyChecklistConfigCommand, Guid>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CopyChecklistConfigCommandHandler(
        IChecklistConfigRepository configRepository,
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _configRepository = configRepository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CopyChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var source = await _configRepository.GetByIdAsync(request.SourceConfigId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), request.SourceConfigId);

        _ = await _semesterRepository.GetByIdAsync(request.TargetSemesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), request.TargetSemesterId);

        var version = await _configRepository.GetMaxVersionForSemesterAsync(request.TargetSemesterId, cancellationToken) + 1;

        var copy = source.CopyTo(request.TargetSemesterId, version, _currentUser.UserId);

        await _configRepository.AddAsync(copy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return copy.Id;
    }
}
