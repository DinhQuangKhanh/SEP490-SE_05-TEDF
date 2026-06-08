using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;

public class UpdateSystemSettingsCommandHandler : ICommandHandler<UpdateSystemSettingsCommand>
{
    private readonly ISettingsDomainService _settings;
    private readonly ICurrentUserService _currentUser;

    public UpdateSystemSettingsCommandHandler(ISettingsDomainService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        await _settings.UpdateSettingsAsync(request.Settings, _currentUser.UserId, cancellationToken);
        return Unit.Value;
    }
}
