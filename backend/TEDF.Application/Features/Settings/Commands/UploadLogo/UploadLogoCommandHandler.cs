using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Settings.Commands.UploadLogo;

public class UploadLogoCommandHandler : ICommandHandler<UploadLogoCommand, string>
{
    private readonly ISettingsDomainService _settings;
    private readonly ICurrentUserService _currentUser;

    public UploadLogoCommandHandler(ISettingsDomainService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    public Task<string> Handle(UploadLogoCommand request, CancellationToken cancellationToken)
        => _settings.UploadLogoAsync(request.Content, request.FileName, request.ContentType, _currentUser.UserId, cancellationToken);
}
