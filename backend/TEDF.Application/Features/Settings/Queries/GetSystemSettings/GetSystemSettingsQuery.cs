using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Features.Settings.Queries.GetSystemSettings;

/// <summary>Admin-only: returns every system setting (all categories).</summary>
public record GetSystemSettingsQuery() : IQuery<List<SystemSettingDto>>;
