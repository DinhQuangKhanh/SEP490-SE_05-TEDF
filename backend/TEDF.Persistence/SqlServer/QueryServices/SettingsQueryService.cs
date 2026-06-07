using System.Reflection;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Settings.DTOs;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Settings feature. See <see cref="ISettingsQueryService"/>.
/// </summary>
public class SettingsQueryService : ISettingsQueryService
{
    private readonly ISystemSettingsService _settings;
    private readonly ISystemConfigurationRepository _repository;

    public SettingsQueryService(ISystemSettingsService settings, ISystemConfigurationRepository repository)
    {
        _settings = settings;
        _repository = repository;
    }

    public async Task<PublicSettingsDto> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
    {
        var primaryColor = await _settings.GetStringAsync(SettingKeys.PrimaryColor, "#2c6090", cancellationToken);
        var headerName = await _settings.GetStringAsync(SettingKeys.HeaderName, "TEDF", cancellationToken);
        var logoUrl = await _settings.GetStringAsync(SettingKeys.LogoUrl, "", cancellationToken);
        var maintenance = await _settings.GetBoolAsync(SettingKeys.MaintenanceMode, false, cancellationToken);

        return new PublicSettingsDto(primaryColor, headerName, logoUrl, maintenance, ResolveVersion());
    }

    public async Task<List<SystemSettingDto>> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return all
            .Select(c => new SystemSettingDto(c.Key, c.Value, c.DataType.ToString(), c.Description, c.Category))
            .ToList();
    }

    private static string ResolveVersion()
    {
        // Resolve the Application assembly version (PublicSettingsDto lives there), preserving the
        // original behaviour after moving this logic out of the handler.
        var assembly = typeof(PublicSettingsDto).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString()
               ?? "1.0.0";
    }
}
