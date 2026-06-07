using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Features.Settings.Queries.GetSystemSettings;

public class GetSystemSettingsQueryHandler : IQueryHandler<GetSystemSettingsQuery, List<SystemSettingDto>>
{
    private readonly ISettingsQueryService _settings;

    public GetSystemSettingsQueryHandler(ISettingsQueryService settings) => _settings = settings;

    public Task<List<SystemSettingDto>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
        => _settings.GetSystemSettingsAsync(cancellationToken);
}
