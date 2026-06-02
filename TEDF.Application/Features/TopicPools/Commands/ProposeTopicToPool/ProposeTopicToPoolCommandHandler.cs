using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Services;
using IUnitOfWork = TEDF.Domain.Common.Interfaces.IUnitOfWork;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Handles ProposeTopicToPoolCommand.
/// Creates a new Project (FromPool) in the given TopicPool.
/// </summary>
public class ProposeTopicToPoolCommandHandler
    : ICommandHandler<ProposeTopicToPoolCommand, Guid>
{
    private readonly ITopicPoolRepository _topicPoolRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ISemesterDomainService _semesterDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public ProposeTopicToPoolCommandHandler(
        ITopicPoolRepository topicPoolRepo,
        IProjectRepository projectRepo,
        ICurrentUserService currentUser,
        ISemesterDomainService semesterDomainService,
        IUnitOfWork unitOfWork)
    {
        _topicPoolRepo = topicPoolRepo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
        _semesterDomainService = semesterDomainService;
        _unitOfWork = unitOfWork;
    }


    public async Task<Guid> Handle(
        ProposeTopicToPoolCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // 1. Get the topic pool
        var pool = await _topicPoolRepo.GetByIdAsync(request.PoolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), request.PoolId);

        // 2. Check pool is accepting proposals
        if (!pool.IsAcceptingProposals())
            throw new BusinessRuleValidationException(
                "This topic pool is not currently accepting new topic proposals.");

        // 3. Generate a unique project code
        var year = DateTime.UtcNow.Year;
        var sequence = await _projectRepo.GetNextSequenceAsync(year, cancellationToken);
        var code = ProjectCode.Generate(year, sequence);

        // 4. Resolve created semester and expiration semester IDs.
        var createdSemesterId = await _semesterDomainService.GetActiveSemesterIdAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ đang hoạt động để tạo đề tài từ topic pool.");

        var expirationOffset = Math.Max(1, pool.ExpirationSemesters);
        var expirationSemesterId = await _semesterDomainService.GetSemesterAfterAsync(
            createdSemesterId,
            expirationOffset,
            cancellationToken);

        var project = Project.CreateFromPool(
            code: code,
            nameVi: ProjectName.Create(request.NameVi),
            nameEn: ProjectName.Create(request.NameEn),
            nameAbbr: request.NameAbbr,
            description: request.Description,
            objectives: request.Objectives,
            scope: request.Scope,
            technologyStack: request.Technologies is not null
                ? TechnologyStack.Create(request.Technologies)
                : null,
            expectedResults: request.ExpectedResults,
            majorId: pool.MajorId,
            semesterId: createdSemesterId,
            maxStudents: request.MaxStudents,
            topicPoolId: pool.Id,
            expirationSemesterId: expirationSemesterId);

        // 5. Assign the current mentor
        project.AddMentor(_currentUser.UserId.Value, assignedBy: _currentUser.UserId.Value);

        // 6. Persist
        await _projectRepo.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
