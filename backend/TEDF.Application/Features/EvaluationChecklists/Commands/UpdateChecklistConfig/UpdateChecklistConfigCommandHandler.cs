using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;

public class UpdateChecklistConfigCommandHandler : ICommandHandler<UpdateChecklistConfigCommand>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateChecklistConfigCommandHandler(
        IChecklistConfigRepository configRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChecklistConfig), request.Id);

        // Domain enforces "Draft only" — an Active/used config must be copied to a new version instead.
        var criteria = request.Criteria.Select(c => (c.TitleVi, c.TitleEn, c.Description));
        config.ReplaceCriteria(criteria, _currentUser.UserId);

        _configRepository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
