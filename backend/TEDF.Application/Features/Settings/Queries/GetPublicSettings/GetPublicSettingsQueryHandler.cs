using System.Reflection;
using TEDF.Application.Common;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Features.Settings.Queries.GetPublicSettings;

public class GetPublicSettingsQueryHandler : IQueryHandler<GetPublicSettingsQuery, PublicSettingsDto>
{
    private readonly ISystemSettingsService _settings;

    public GetPublicSettingsQueryHandler(ISystemSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<PublicSettingsDto> Handle(GetPublicSettingsQuery request, CancellationToken cancellationToken)
    {
        var primaryColor = await _settings.GetStringAsync(SettingKeys.PrimaryColor, "#2c6090", cancellationToken);
        var headerName = await _settings.GetStringAsync(SettingKeys.HeaderName, "TEDF", cancellationToken);
        var logoUrl = await _settings.GetStringAsync(SettingKeys.LogoUrl, "", cancellationToken);
        var maintenance = await _settings.GetBoolAsync(SettingKeys.MaintenanceMode, false, cancellationToken);

        return new PublicSettingsDto(primaryColor, headerName, logoUrl, maintenance, ResolveVersion());
    }

    private static string ResolveVersion()
    {
        var assembly = typeof(GetPublicSettingsQueryHandler).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString()
               ?? "1.0.0";
    }
}
