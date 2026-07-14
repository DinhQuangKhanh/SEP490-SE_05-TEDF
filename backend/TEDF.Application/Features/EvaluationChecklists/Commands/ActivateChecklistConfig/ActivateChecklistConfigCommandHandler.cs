using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ActivateChecklistConfig;

public class ActivateChecklistConfigCommandHandler : ICommandHandler<ActivateChecklistConfigCommand>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ActivateChecklistConfigCommandHandler(
        IChecklistConfigRepository configRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ActivateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), request.Id);

        // Retire the current Active config for this semester so only one stays Active.
        var currentActive = await _configRepository.GetActiveBySemesterAsync(config.SemesterId, cancellationToken);
        if (currentActive is not null && currentActive.Id != config.Id)
        {
            currentActive.Deactivate(_currentUser.UserId);
            _configRepository.Update(currentActive);
        }

        // Domain enforces the exactly-10-criteria rule here.
        config.Activate(_currentUser.UserId);
        _configRepository.Update(config);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
