using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Settings.DTOs;
using TEDF.Domain.Entities;

namespace TEDF.Application.Features.Settings.Queries.GetSystemSettings;

public class GetSystemSettingsQueryHandler : IQueryHandler<GetSystemSettingsQuery, List<SystemSettingDto>>
{
    private readonly ISystemConfigurationRepository _repository;

    public GetSystemSettingsQueryHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SystemSettingDto>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return all
            .Select(c => new SystemSettingDto(c.Key, c.Value, c.DataType.ToString(), c.Description, c.Category))
            .ToList();
    }
}
