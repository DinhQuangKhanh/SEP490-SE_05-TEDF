using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;

public class CreateChecklistConfigCommandHandler : ICommandHandler<CreateChecklistConfigCommand, Guid>
{
    private readonly IChecklistConfigRepository _configRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateChecklistConfigCommandHandler(
        IChecklistConfigRepository configRepository,
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _configRepository = configRepository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateChecklistConfigCommand request, CancellationToken cancellationToken)
    {
        _ = await _semesterRepository.GetByIdAsync(request.SemesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), request.SemesterId);

        var version = await _configRepository.GetMaxVersionForSemesterAsync(request.SemesterId, cancellationToken) + 1;

        var criteria = request.Criteria.Select(c => (c.TitleVi, c.TitleEn, c.Description));
        var config = ChecklistConfig.Create(request.SemesterId, version, criteria, createdBy: _currentUser.UserId);

        await _configRepository.AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return config.Id;
    }
}
