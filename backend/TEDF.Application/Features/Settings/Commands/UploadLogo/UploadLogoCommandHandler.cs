using TEDF.Application.Common;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;
using IFileStorageService = TEDF.Application.Common.Interfaces.IFileStorageService;

namespace TEDF.Application.Features.Settings.Commands.UploadLogo;

public class UploadLogoCommandHandler : ICommandHandler<UploadLogoCommand, string>
{
    private readonly IFileStorageService _fileStorage;
    private readonly ISystemConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadLogoCommandHandler(
        IFileStorageService fileStorage,
        ISystemConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _fileStorage = fileStorage;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(UploadLogoCommand request, CancellationToken cancellationToken)
    {
        var result = await _fileStorage.UploadAsync(request.Content, request.FileName, "branding", cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.PublicUrl))
            throw new DomainException(result.Error ?? "Tải logo lên thất bại.");

        var config = await _repository.GetByKeyAsync(SettingKeys.LogoUrl, cancellationToken);
        if (config is not null)
        {
            config.UpdateValue(result.PublicUrl, _currentUser.UserId);
            _repository.Update(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result.PublicUrl;
    }
}
