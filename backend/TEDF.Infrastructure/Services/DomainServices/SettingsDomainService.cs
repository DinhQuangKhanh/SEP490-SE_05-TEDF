using TEDF.Application.Common;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Services;
using IEmailSender = TEDF.Application.Common.Interfaces.IEmailSender;
using IFileStorageService = TEDF.Application.Common.Interfaces.IFileStorageService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Settings feature. See <see cref="ISettingsDomainService"/>.
/// </summary>
public class SettingsDomainService : ISettingsDomainService
{
    private readonly IEmailSender _emailSender;
    private readonly ISystemConfigurationRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public SettingsDomainService(
        IEmailSender emailSender,
        ISystemConfigurationRepository repository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork)
    {
        _emailSender = emailSender;
        _repository = repository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public Task SendTestEmailAsync(string toEmail, string? recipientName, CancellationToken cancellationToken = default)
    {
        const string subject = "TEDF — Email kiểm tra cấu hình";
        var body = $"<p>Xin chào {recipientName ?? "Quản trị viên"},</p>"
                 + "<p>Đây là email kiểm tra được gửi từ trang Cấu hình hệ thống của TEDF. "
                 + "Nếu bạn nhận được email này, cấu hình gửi email đang hoạt động bình thường.</p>"
                 + $"<p><small>Thời điểm gửi: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</small></p>";

        return _emailSender.SendAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task UpdateSettingsAsync(IReadOnlyDictionary<string, string> settings, Guid? updatedBy, CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in settings)
        {
            var config = await _repository.GetByKeyAsync(key, cancellationToken);
            if (config is null) continue; // ignore unknown keys

            config.UpdateValue(value ?? string.Empty, updatedBy);
            _repository.Update(config);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> UploadLogoAsync(Stream content, string fileName, string contentType, Guid? updatedBy, CancellationToken cancellationToken = default)
    {
        var result = await _fileStorage.UploadAsync(content, fileName, "branding", cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.PublicUrl))
            throw new DomainException(result.Error ?? "Tải logo lên thất bại.");

        var config = await _repository.GetByKeyAsync(SettingKeys.LogoUrl, cancellationToken);
        if (config is not null)
        {
            config.UpdateValue(result.PublicUrl, updatedBy);
            _repository.Update(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result.PublicUrl;
    }
}
