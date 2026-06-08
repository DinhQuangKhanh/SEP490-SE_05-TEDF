namespace TEDF.Application.Features.Settings.DTOs;

/// <summary>
/// The non-secret subset of settings any client may read at startup (branding + maintenance).
/// Served anonymously by GET /api/settings/public.
/// </summary>
public record PublicSettingsDto(
    string PrimaryColor,
    string HeaderName,
    string LogoUrl,
    bool MaintenanceMode,
    string Version);
