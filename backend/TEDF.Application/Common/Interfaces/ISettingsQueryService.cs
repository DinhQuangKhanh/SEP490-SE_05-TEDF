using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Settings feature. Query handlers depend on this only.
/// </summary>
public interface ISettingsQueryService
{
    Task<PublicSettingsDto> GetPublicSettingsAsync(CancellationToken cancellationToken = default);
    Task<List<SystemSettingDto>> GetSystemSettingsAsync(CancellationToken cancellationToken = default);
}
