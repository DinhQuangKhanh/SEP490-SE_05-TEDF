using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigs;

public class GetChecklistConfigsQueryHandler : IQueryHandler<GetChecklistConfigsQuery, ChecklistConfigListDto>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;

    public GetChecklistConfigsQueryHandler(
        IChecklistConfigRepository configRepository,
        ISemesterRepository semesterRepository,
        IUserRepository userRepository)
    {
        _configRepository = configRepository;
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
    }

    public async Task<ChecklistConfigListDto> Handle(GetChecklistConfigsQuery request, CancellationToken cancellationToken)
    {
        var semesters = (await _semesterRepository.GetAllAsync(cancellationToken)).ToList();
        var semesterNameById = semesters.ToDictionary(s => s.Id, s => s.Name);

        var semesterOptions = semesters
            .OrderByDescending(s => s.Id)
            .Select(s => new ChecklistSemesterOptionDto(s.Id, s.Name, s.Code.Value, s.Status.ToString()))
            .ToList();

        var configs = request.SemesterId.HasValue
            ? await _configRepository.GetBySemesterAsync(request.SemesterId.Value, cancellationToken)
            : await _configRepository.GetAllOrderedAsync(cancellationToken);

        var userNames = await ResolveUserNamesAsync(configs, cancellationToken);

        var configDtos = new List<ChecklistConfigDto>(configs.Count);
        foreach (var config in configs)
        {
            var isUsed = await _configRepository.HasResultsAsync(config.Id, cancellationToken);
            var semesterName = semesterNameById.TryGetValue(config.SemesterId, out var name) ? name : $"#{config.SemesterId}";
            configDtos.Add(ChecklistConfigMapper.ToDto(config, semesterName, isUsed, userNames));
        }

        return new ChecklistConfigListDto(semesterOptions, configDtos);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveUserNamesAsync(
        IReadOnlyList<ChecklistConfig> configs, CancellationToken cancellationToken)
    {
        var ids = configs
            .SelectMany(c => new[] { c.CreatedBy, c.UpdatedBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }
}
