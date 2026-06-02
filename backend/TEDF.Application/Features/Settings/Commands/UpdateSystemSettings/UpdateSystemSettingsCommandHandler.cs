using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;

public class UpdateSystemSettingsCommandHandler : ICommandHandler<UpdateSystemSettingsCommand>
{
    private readonly ISystemConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateSystemSettingsCommandHandler(
        ISystemConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        var updatedBy = _currentUser.UserId;

        foreach (var (key, value) in request.Settings)
        {
            var config = await _repository.GetByKeyAsync(key, cancellationToken);
            if (config is null) continue; // ignore unknown keys

            config.UpdateValue(value ?? string.Empty, updatedBy);
            _repository.Update(config);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
