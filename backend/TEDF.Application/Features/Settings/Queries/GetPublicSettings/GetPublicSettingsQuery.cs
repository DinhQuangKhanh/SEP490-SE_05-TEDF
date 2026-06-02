using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Features.Settings.Queries.GetPublicSettings;

/// <summary>Anonymous: branding + maintenance flag the SPA reads at startup. No secrets.</summary>
public record GetPublicSettingsQuery() : IQuery<PublicSettingsDto>;
