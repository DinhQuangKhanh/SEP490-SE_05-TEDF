using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigById;

public class GetChecklistConfigByIdQueryHandler : IQueryHandler<GetChecklistConfigByIdQuery, ChecklistConfigDto?>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;

    public GetChecklistConfigByIdQueryHandler(
        IChecklistConfigRepository configRepository,
        ISemesterRepository semesterRepository,
        IUserRepository userRepository)
    {
        _configRepository = configRepository;
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
    }

    public async Task<ChecklistConfigDto?> Handle(GetChecklistConfigByIdQuery request, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByIdAsync(request.Id, cancellationToken);
        if (config is null)
            return null;

        var semester = await _semesterRepository.GetByIdAsync(config.SemesterId, cancellationToken);
        var isUsed = await _configRepository.HasResultsAsync(config.Id, cancellationToken);

        var ids = new[] { config.CreatedBy, config.UpdatedBy }
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var userNames = ids.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _userRepository.GetByIdsAsync(ids, cancellationToken)).ToDictionary(u => u.Id, u => u.FullName);

        return ChecklistConfigMapper.ToDto(config, semester?.Name ?? $"#{config.SemesterId}", isUsed, userNames);
    }
}
