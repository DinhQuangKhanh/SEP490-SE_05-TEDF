using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Settings.DTOs;

namespace TEDF.Application.Features.Settings.Queries.GetPublicSettings;

public class GetPublicSettingsQueryHandler : IQueryHandler<GetPublicSettingsQuery, PublicSettingsDto>
{
    private readonly ISettingsQueryService _settings;

    public GetPublicSettingsQueryHandler(ISettingsQueryService settings) => _settings = settings;

    public Task<PublicSettingsDto> Handle(GetPublicSettingsQuery request, CancellationToken cancellationToken)
        => _settings.GetPublicSettingsAsync(cancellationToken);
}
