using TEDF.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.CreateGroup;

public class CreateGroupCommandHandler : ICommandHandler<CreateGroupCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateGroupCommandHandler(
        IGroupRepository groupRepository,
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _groupRepository = groupRepository;
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Get next semester
        var nextSemester = await _semesterRepository.GetNextSemesterAsync(null, cancellationToken)
            ?? throw new BusinessRuleValidationException("No next semester found.");

        // Check if student is already in an active group next semester
        if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, nextSemester.Id, cancellationToken))
            throw new BusinessRuleValidationException("Student is already in an active group next semester.");

        if (await _groupRepository.HasPendingJoinRequestAsync(studentId, nextSemester.Id, cancellationToken))
            throw new BusinessRuleValidationException("Student has a pending join request and cannot create a new group yet.");

        // Generate group code
        var year = DateTime.UtcNow.Year;
        var seq = await _groupRepository.GetNextSequenceAsync(year, cancellationToken);
        var code = GroupCode.Generate(year, seq);

        // Create group with current student as leader
        var group = Group.Create(code, nextSemester.Id, studentId, request.Name);

        await _groupRepository.AddAsync(group, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsGroupCodeUniqueViolation(ex))
        {
            throw new ConcurrencyException(nameof(Group), code.Value);
        }

        return group.Id;
    }

    private static bool IsGroupCodeUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_Groups_Code", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("IX_Groups_Code", StringComparison.OrdinalIgnoreCase);
    }
}
