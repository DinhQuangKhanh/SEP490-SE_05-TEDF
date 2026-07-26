using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the DirectTopics feature. See <see cref="IDirectTopicsQueryService"/>.
/// </summary>
public class DirectTopicsQueryService : IDirectTopicsQueryService
{
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ITopicRegistrationRepository _registrationRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly ICurrentUserService _currentUser;

    public DirectTopicsQueryService(
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        ITopicRegistrationRepository registrationRepository,
        IMajorReadRepository majorRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _registrationRepository = registrationRepository;
        _majorRepository = majorRepository;
        _currentUser = currentUser;
    }

    public async Task<AvailableMentorsResponse> GetAvailableMentorsAsync(CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var activeSemester = await _semesterRepository.GetActiveAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ đang hoạt động.");

        var nextSemester = await _semesterRepository.GetSemesterAfterAsync(activeSemester.Id, 1, cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ kế tiếp.");

        // The student's program is fixed by the eligible-student roster (the major is read-only on the form).
        var studentMajorId = await _semesterRepository.GetEligibleStudentMajorAsync(studentId, nextSemester.Id, cancellationToken)
            ?? throw new BusinessRuleValidationException("Bạn chưa được gán chuyên ngành trong học kỳ này.");

        var major = await _majorRepository.GetByIdAsync(studentMajorId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Major), studentMajorId);

        // Only mentors rostered to supervise this major this semester — not every "Mentor" user.
        var mentorIds = await _semesterRepository.GetEligibleMentorIdsByMajorAsync(nextSemester.Id, studentMajorId, cancellationToken);
        var mentors = await _userRepository.GetByIdsAsync(mentorIds, cancellationToken);

        var result = new List<AvailableMentorDto>();
        foreach (var mentor in mentors)
        {
            // A pending pool registration reserves a topic that becomes one supervised group on confirm,
            // so it counts toward capacity here (the proposal screen), as required by the business rules.
            var activeCount = await _projectRepository.CountMentorActiveProjectsInSemesterAsync(
                mentor.Id, nextSemester.Id, cancellationToken);
            var pendingPoolCount = await _registrationRepository.CountPendingByMentorIdAsync(mentor.Id, cancellationToken);

            result.Add(new AvailableMentorDto(
                mentor.Id,
                mentor.FullName,
                mentor.Email?.Value ?? "",
                mentor.Lecturer?.AcademicTitle,
                activeCount + pendingPoolCount,
                MentorCannotExceedMaxGroupsPerSemesterRule.MaxGroupsPerSemester));
        }

        var ordered = result.OrderBy(m => m.CurrentGroupCount).ThenBy(m => m.FullName).ToList();
        return new AvailableMentorsResponse(studentMajorId, major.Name, ordered);
    }
}
