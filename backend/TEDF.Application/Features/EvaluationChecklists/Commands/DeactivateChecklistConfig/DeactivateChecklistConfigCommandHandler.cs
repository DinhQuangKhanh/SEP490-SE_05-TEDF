using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.DeactivateChecklistConfig;

public class DeactivateChecklistConfigCommandHandler : ICommandHandler<DeactivateChecklistConfigCommand>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeactivateChecklistConfigCommandHandler(
        IChecklistConfigRepository configRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeactivateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), request.Id);

        config.Deactivate(_currentUser.UserId);
        _configRepository.Update(config);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
