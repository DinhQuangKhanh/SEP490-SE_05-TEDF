using TEDF.Domain.Aggregates.ProjectAggregate.Entities;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Document;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.ProjectAggregate
{
    public class Project : AggregateRoot<Guid>
    {
        private readonly List<ProjectMentor> _mentors = [];
        private readonly List<Document> _documents = [];
        private readonly List<ProposedGroupMember> _proposedMembers = [];

        #region Properties

        public ProjectCode Code { get; private set; } = null!;
        public ProjectName NameVi { get; private set; } = null!;
        public ProjectName NameEn { get; private set; } = null!;
        public string NameAbbr { get; private set; } = null!;
        public string Description { get; private set; } = string.Empty;
        public string Objectives { get; private set; } = string.Empty;
        public string? Scope { get; private set; }
        public TechnologyStack? Technologies { get; private set; }
        public string? ExpectedResults { get; private set; }
        public int MajorId { get; private set; }

        /// <summary>
        /// The semester this topic is carried out in — the one its group belongs to, and the one
        /// every "filter by semester" screen matches on. For a pool topic this is the semester whose
        /// Registration phase was open when the mentor proposed it, which is the semester *after*
        /// the one that was running (see <see cref="CreatedInSemesterId"/>).
        /// </summary>
        public int SemesterId { get; private set; }
        public Guid? GroupId { get; private set; }
        public Guid? TopicPoolId { get; private set; }
        public int MaxStudents { get; private set; }
        public ProjectSourceType SourceType { get; private set; }
        public RegistrationType RegistrationType { get; private set; }
        public ProjectStatus Status { get; private set; }
        public ProjectPriority Priority { get; private set; }
        public DateTime? SubmittedAt { get; private set; }
        public Guid? SubmittedBy { get; private set; }
        public DateTime? ApprovedAt { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? Deadline { get; private set; }
        public int EvaluationCount { get; private set; }
        public EvaluationResult? LastEvaluationResult { get; private set; }
        /// <summary>
        /// A mentor review note from the retired student-proposal flow. Nothing writes it any more,
        /// but the setter has to stay: EF drops the column without it, and historical projects
        /// still surface their note.
        /// </summary>
        public string? MentorFeedback { get; private set; }

        /// <summary>
        /// Rich-text note (sanitized HTML) the mentor writes when proposing the topic — e.g. capability
        /// requirements for students who register. Set from the propose modal (React Quill). Optional.
        /// </summary>
        public string? MentorNote { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public PoolTopicStatus? PoolStatus { get; private set; }

        /// <summary>
        /// The semester that was running when this pool topic was proposed — one earlier than
        /// <see cref="SemesterId"/>. Null for topics proposed between semesters, and for the
        /// historical rows written before the two were told apart.
        /// </summary>
        public int? CreatedInSemesterId { get; private set; }
        public int? ExpirationSemesterId { get; private set; }

        public IReadOnlyCollection<ProjectMentor> Mentors => _mentors.AsReadOnly();
        public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

        /// <summary>
        /// Students listed on the register form the mentor attached when proposing this topic.
        /// Empty when no roster was supplied — the topic then follows the normal pool flow.
        /// </summary>
        public IReadOnlyCollection<ProposedGroupMember> ProposedMembers => _proposedMembers.AsReadOnly();

        /// <summary>
        /// Gets the count of currently active mentors (avoids materializing a list).
        /// </summary>
        public int ActiveMentorCount => _mentors.Count(m => m.IsActive);

        #endregion

        #region Constructors

        private Project() { }

        #endregion

        #region Factory Methods

        /// <param name="semesterId">
        /// The semester the topic will be carried out in. Topics are proposed during the previous
        /// semester, so this is <b>not</b> the semester that is running at proposal time.
        /// </param>
        /// <param name="createdInSemesterId">
        /// The semester that was running when the topic was proposed, kept for the pool-expiry
        /// audit trail. Null when the proposal falls in the gap between two semesters.
        /// </param>
        public static Project CreateFromPool(ProjectCode code, ProjectName nameVi, ProjectName nameEn, string nameAbbr, string description, string objectives,
            string? scope, TechnologyStack? technologyStack, string? expectedResults, int majorId, int semesterId, int maxStudents, Guid topicPoolId,
            int? createdInSemesterId, int? expirationSemesterId)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Code = code,
                NameVi = nameVi,
                NameEn = nameEn,
                NameAbbr = nameAbbr,
                Description = description,
                Objectives = objectives,
                Scope = scope,
                Technologies = technologyStack,
                ExpectedResults = expectedResults,
                MajorId = majorId,
                SemesterId = semesterId,
                MaxStudents = maxStudents,
                SourceType = ProjectSourceType.FromPool,
                TopicPoolId = topicPoolId,
                GroupId = null,                          // No group yet
                PoolStatus = PoolTopicStatus.Available,  // Available for registration
                CreatedInSemesterId = createdInSemesterId,
                ExpirationSemesterId = expirationSemesterId,
                Status = ProjectStatus.PendingEvaluation,
                Priority = ProjectPriority.Normal,
                RegistrationType = RegistrationType.Public,
                EvaluationCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            project.RaiseDomainEvent(new ProjectCreatedEvent(project.Id, project.Code.Value, ProjectSourceType.FromPool));
            return project;
        }

        /// <summary>
        /// Sets the mentor's rich-text note (sanitized HTML) captured when proposing the topic. Null or
        /// blank clears it. Kept separate from the factory so the note can be attached after creation.
        /// </summary>
        public void SetMentorNote(string? note)
        {
            MentorNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records the students listed on the register form attached at proposal time.
        /// Replaces any previously recorded roster. Passing an empty roster clears it, which
        /// leaves the topic on the normal pool flow.
        /// </summary>
        /// <param name="members">Student ids paired with whether they are the group leader.</param>
        public void SetProposedRoster(IEnumerable<(Guid StudentId, bool IsLeader)> members)
        {
            ArgumentNullException.ThrowIfNull(members);

            var roster = members
                .GroupBy(m => m.StudentId)
                .Select(g => (StudentId: g.Key, IsLeader: g.Any(m => m.IsLeader)))
                .ToList();

            if (roster.Count > MaxStudents)
                throw new BusinessRuleValidationException($"Danh sách sinh viên vượt quá số lượng tối đa ({MaxStudents}).");

            if (roster.Count(m => m.IsLeader) > 1)
                throw new BusinessRuleValidationException("Chỉ được chỉ định một nhóm trưởng.");

            _proposedMembers.Clear();
            foreach (var (studentId, isLeader) in roster)
                _proposedMembers.Add(ProposedGroupMember.Create(Id, studentId, isLeader));

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the pool status for projects from the topic pool.
        /// </summary>
        public void SetPoolStatus(PoolTopicStatus status)
        {
            if (SourceType != ProjectSourceType.FromPool)
                throw new BusinessRuleValidationException("Pool status can only be set for projects from the topic pool.");

            PoolStatus = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetExpirationSemester(int expirationSemesterId)
        {
            if (SourceType != ProjectSourceType.FromPool)
                throw new BusinessRuleValidationException("Expiration semester can only be set for projects from the topic pool.");

            if (expirationSemesterId <= 0)
                throw new BusinessRuleValidationException("Expiration semester must be a positive identifier.");

            ExpirationSemesterId = expirationSemesterId;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Mentor Management

        public void AddMentor(Guid mentorId, Guid assignedBy)
        {
            CheckRule(new ProjectCannotExceedMaxMentorsRule(ActiveMentorCount));
            if (_mentors.Any(m => m.MentorId == mentorId && m.IsActive))
                throw new BusinessRuleValidationException("Mentor is already assigned to this project.");

            var mentor = ProjectMentor.Create(Id, mentorId, assignedBy);
            _mentors.Add(mentor);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new MentorAssignedEvent(Id, mentorId));
        }

        public void RemoveMentor(Guid mentorId)
        {
            var mentor = _mentors.FirstOrDefault(m => m.MentorId == mentorId && m.IsActive)
                ?? throw new EntityNotFoundException(nameof(ProjectMentor), mentorId);
            CheckRule(new ProjectMustHaveAtLeastOneMentorRule(ActiveMentorCount - 1));
            mentor.Deactivate();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new MentorRemovedEvent(Id, mentorId));
        }

        #endregion

        #region Evaluation Workflow

        public void SubmitForEvaluation(Guid submittedBy)
        {
            CheckRule(new ProjectCanOnlyBeSubmittedWhenDraftRule(Status));
            Status = ProjectStatus.PendingEvaluation;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
            EvaluationCount++;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectSubmittedEvent(Id, submittedBy, EvaluationCount));
        }

        public void Approve()
        {
            if (Status != ProjectStatus.PendingEvaluation)
                throw new BusinessRuleValidationException("Only projects pending evaluation can be approved.");
            Status = ProjectStatus.Approved;
            ApprovedAt = DateTime.UtcNow;
            LastEvaluationResult = EvaluationResult.Approved;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectApprovedEvent(Id));
        }

        public void RequestModification()
        {
            if (Status != ProjectStatus.PendingEvaluation)
                throw new BusinessRuleValidationException("Only projects pending evaluation can request modification.");
            Status = ProjectStatus.NeedsModification;
            LastEvaluationResult = EvaluationResult.NeedsModification;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectModificationRequestedEvent(Id));
        }

        public void Reject()
        {
            if (Status != ProjectStatus.PendingEvaluation)
                throw new BusinessRuleValidationException("Only projects pending evaluation can be rejected.");
            Status = ProjectStatus.Rejected;
            LastEvaluationResult = EvaluationResult.Rejected;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectRejectedEvent(Id));
        }

        public void UpdateAfterFeedback(string? description = null, string? objectives = null, string? scope = null,
            string? technologies = null, string? expectedResults = null)
        {
            CheckRule(new ProjectCanOnlyBeModifiedWhenNeedsModificationRule(Status));
            if (!string.IsNullOrWhiteSpace(description)) Description = description;
            if (!string.IsNullOrWhiteSpace(objectives)) Objectives = objectives;
            if (scope != null) Scope = scope;
            if (!string.IsNullOrWhiteSpace(technologies)) Technologies = TechnologyStack.Create(technologies);
            if (expectedResults != null) ExpectedResults = expectedResults;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectModifiedEvent(Id));
        }

        public void Resubmit(Guid submittedBy)
        {
            CheckRule(new ProjectCanOnlyBeSubmittedWhenDraftRule(Status, allowNeedsModification: true));
            Status = ProjectStatus.PendingEvaluation;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
            EvaluationCount++;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectResubmittedEvent(Id, submittedBy, EvaluationCount));
        }

        #endregion

        #region Project Lifecycle

        /// <summary>
        /// Assigns a group to this project (used when group registers for a topic from pool).
        /// </summary>
        public void AssignGroup(Guid groupId)
        {
            if (Status != ProjectStatus.Approved && Status != ProjectStatus.InProgress && Status != ProjectStatus.Completed)
                throw new BusinessRuleValidationException(
                    "Cannot assign a group to a project that has not been approved.");

            if (GroupId.HasValue)
                throw new BusinessRuleValidationException("Project already has a group assigned.");

            GroupId = groupId;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ProjectGroupAssignedEvent(Id, groupId));
        }

        public void StartProgress(DateTime startDate, DateTime deadline)
        {
            if (Status != ProjectStatus.Approved)
                throw new BusinessRuleValidationException("Only approved projects can start progress.");
            if (deadline <= startDate)
                throw new BusinessRuleValidationException("Deadline must be after start date.");
            Status = ProjectStatus.InProgress;
            StartDate = startDate;
            Deadline = deadline;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectStartedEvent(Id, startDate, deadline));
        }

        public void Complete()
        {
            if (Status != ProjectStatus.InProgress)
                throw new BusinessRuleValidationException("Only in-progress projects can be completed.");
            Status = ProjectStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectCompletedEvent(Id));
        }

        public void Cancel()
        {
            if (Status == ProjectStatus.Completed)
                throw new BusinessRuleValidationException("Completed projects cannot be cancelled.");
            Status = ProjectStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectCancelledEvent(Id));
        }

        /// <summary>
        /// Marks this topic as expired (called when expiration semester is reached without registration).
        /// </summary>
        public void MarkAsExpired()
        {
            if (SourceType != ProjectSourceType.FromPool)
                throw new BusinessRuleValidationException("Only pool topics can expire.");

            if (PoolStatus != PoolTopicStatus.Available)
                throw new BusinessRuleValidationException("Only available topics can be marked as expired.");

            PoolStatus = PoolTopicStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Document Management

        public Document AddDocument(string fileName, string originalFileName, string fileType, long fileSize,
            string filePath, DocumentType documentType, Guid uploadedBy)
        {
            var document = Document.Create(Id, fileName, originalFileName, fileType, fileSize, filePath, documentType, uploadedBy);
            _documents.Add(document);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new DocumentUploadedEvent(Id, document.Id, documentType));
            return document;
        }

        public void RemoveDocument(Guid documentId, Guid deletedBy)
        {
            var document = _documents.FirstOrDefault(d => d.Id == documentId && !d.IsDeleted)
                ?? throw new EntityNotFoundException(nameof(Document), documentId);
            document.Delete();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new DocumentDeletedEvent(Id, documentId, deletedBy));
        }

        #endregion

        #region Update Methods

        public void UpdateBasicInfo(ProjectName? nameVi = null, ProjectName? nameEn = null, string? nameAbbr = null,
            string? description = null, string? objectives = null, string? scope = null, string? technologies = null, string? expectedResults = null)
        {
            if (Status != ProjectStatus.Draft && Status != ProjectStatus.NeedsModification)
                throw new BusinessRuleValidationException("Project can only be updated when in Draft or NeedsModification status.");
            if (nameVi != null) NameVi = nameVi;
            if (nameEn != null) NameEn = nameEn;
            if (nameAbbr != null) NameAbbr = nameAbbr;
            if (!string.IsNullOrWhiteSpace(description)) Description = description;
            if (!string.IsNullOrWhiteSpace(objectives)) Objectives = objectives;
            if (scope != null) Scope = scope;
            if (!string.IsNullOrWhiteSpace(technologies)) Technologies = TechnologyStack.Create(technologies);
            if (expectedResults != null) ExpectedResults = expectedResults;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPriority(ProjectPriority priority) { Priority = priority; UpdatedAt = DateTime.UtcNow; }
        public void SetMaxStudents(int maxStudents)
        {
            if (maxStudents is < 1 or > 5)
                throw new BusinessRuleValidationException("Maximum students must be between 1 and 5.");
            MaxStudents = maxStudents;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        /// <summary>
        /// Checks if this topic is expired based on current semester.
        /// </summary>
        public bool IsExpired(int currentSemesterId)
        {
            if (SourceType != ProjectSourceType.FromPool)
                return false;

            return ExpirationSemesterId.HasValue && currentSemesterId > ExpirationSemesterId.Value;
        }
    }
}
