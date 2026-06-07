namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the Settings feature. Command handlers depend on this only.
/// </summary>
public interface ISettingsDomainService
{
    /// <summary>Sends a configuration test email to the given address.</summary>
    Task SendTestEmailAsync(string toEmail, string? recipientName, CancellationToken cancellationToken = default);

    /// <summary>Upserts existing settings keys (unknown keys are ignored).</summary>
    Task UpdateSettingsAsync(IReadOnlyDictionary<string, string> settings, Guid? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>Uploads a new system logo and stores its public URL; returns that URL.</summary>
    Task<string> UploadLogoAsync(Stream content, string fileName, string contentType, Guid? updatedBy, CancellationToken cancellationToken = default);
}
