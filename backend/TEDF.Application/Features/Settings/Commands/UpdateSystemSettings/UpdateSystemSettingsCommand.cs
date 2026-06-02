using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;

/// <summary>
/// Admin-only: upserts the supplied settings (key → value). Only keys that already exist in the
/// SystemConfiguration store are updated; unknown keys are ignored. Invalidates the cached
/// settings snapshot on success.
/// </summary>
public record UpdateSystemSettingsCommand(Dictionary<string, string> Settings) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => new[] { "settings:" };
}
