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
        /// <summary>The mentor's most recent modification request note, shown to the student. Cleared on resubmit/approve.</summary>
        public string? MentorFeedback { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public PoolTopicStatus? PoolStatus { get; private set; }
        public int? CreatedInSemesterId { get; private set; }
        public int? ExpirationSemesterId { get; private set; }

        public IReadOnlyCollection<ProjectMentor> Mentors => _mentors.AsReadOnly();
        public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

        /// <summary>
        /// Gets the count of currently active mentors (avoids materializing a list).
        /// </summary>
        public int ActiveMentorCount => _mentors.Count(m => m.IsActive);

        #endregion

        #region Constructors

        private Project() { }

        private Project(Guid id, ProjectCode code, ProjectName nameVi, ProjectName nameEn, string nameAbbr, string description, string objectives,
            string? scope, TechnologyStack? technologyStack, string? expectedResults, int majorId, int semesterId, int maxStudents, ProjectSourceType sourceType, Guid? groupId = null, Guid? topicPoolId = null) : base(id)
        {
            Code = code;
            NameVi = nameVi;
            NameEn = nameEn;
            NameAbbr = nameAbbr;
            Description = description;
            Objectives = objectives;
            Scope = scope;
            Technologies = technologyStack;
            ExpectedResults = expectedResults;
            MajorId = majorId;
            SemesterId = semesterId;
            MaxStudents = maxStudents;
            SourceType = sourceType;
            GroupId = groupId;
            TopicPoolId = topicPoolId;
            Status = ProjectStatus.Draft;
            Priority = ProjectPriority.Normal;
            RegistrationType = RegistrationType.Public;
            EvaluationCount = 0;
            CreatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Factory Methods

        public static Project CreateFromPool(ProjectCode code, ProjectName nameVi, ProjectName nameEn, string nameAbbr, string description, string objectives,
            string? scope, TechnologyStack? technologyStack, string? expectedResults, int majorId, int semesterId, int maxStudents, Guid topicPoolId, int? expirationSemesterId)
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
                CreatedInSemesterId = semesterId,
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

        public static Project CreateDirect(ProjectCode code, ProjectName nameVi, ProjectName nameEn, string nameAbbr, string description, string objectives,
             string? scope, TechnologyStack? technologyStack, string? expectedResults, int majorId, int semesterId, int maxStudents, Guid? groupId = null)
        {
            var project = new Project(Guid.NewGuid(), code, nameVi, nameEn, nameAbbr, description, objectives, scope, technologyStack, expectedResults, majorId, semesterId, maxStudents, ProjectSourceType.DirectRegistration, groupId: groupId);
            project.RaiseDomainEvent(new ProjectCreatedEvent(project.Id, project.Code.Value, ProjectSourceType.DirectRegistration));
            return project;
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

        #region Mentor Review Workflow (Direct Registration)

        /// <summary>
        /// Student submits a direct-registration project to their mentor for review.
        /// Transitions: Draft → PendingMentorReview
        /// </summary>
        public void SubmitToMentor(Guid submittedBy)
        {
            if (Status != ProjectStatus.Draft)
                throw new BusinessRuleValidationException("Project can only be submitted to mentor when in Draft status.");
            if (SourceType != ProjectSourceType.DirectRegistration)
                throw new BusinessRuleValidationException("Only direct registration projects can be submitted to mentor for review.");

            Status = ProjectStatus.PendingMentorReview;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
            MentorFeedback = null; // fresh submission to mentor; clear any prior modification note
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectSubmittedToMentorEvent(Id, submittedBy));
        }

        /// <summary>
        /// Student re-submits a modified direct-registration project back to mentor.
        /// Transitions: NeedsModification → PendingMentorReview
        /// </summary>
        public void ResubmitToMentor(Guid submittedBy)
        {
            if (Status != ProjectStatus.NeedsModification)
                throw new BusinessRuleValidationException("Project can only be resubmitted when in NeedsModification status.");
            if (SourceType != ProjectSourceType.DirectRegistration)
                throw new BusinessRuleValidationException("Only direct registration projects can be resubmitted to mentor.");

            Status = ProjectStatus.PendingMentorReview;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
            MentorFeedback = null; // fresh submission to mentor; clear any prior modification note
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectSubmittedToMentorEvent(Id, submittedBy));
        }

        /// <summary>
        /// Mentor approves the project and submits it for formal evaluation.
        /// Transitions: PendingMentorReview → PendingEvaluation
        /// </summary>
        public void MentorApproveAndSubmit(Guid mentorId)
        {
            if (Status != ProjectStatus.PendingMentorReview)
                throw new BusinessRuleValidationException("Only projects pending mentor review can be approved by mentor.");

            Status = ProjectStatus.PendingEvaluation;
            EvaluationCount++;
            MentorFeedback = null; // approved; the modification note no longer applies
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectMentorApprovedEvent(Id, mentorId));
        }

        /// <summary>
        /// Mentor requests modifications to the project before it can be submitted for evaluation.
        /// Transitions: PendingMentorReview → NeedsModification
        /// </summary>
        public void MentorRequestModification(string? feedback = null)
        {
            if (Status != ProjectStatus.PendingMentorReview)
                throw new BusinessRuleValidationException("Only projects pending mentor review can request modification.");

            Status = ProjectStatus.NeedsModification;
            LastEvaluationResult = EvaluationResult.NeedsModification;
            MentorFeedback = feedback; // surfaced to the student on the my-topic page
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new ProjectMentorRequestedModificationEvent(Id, feedback));
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

        public void RemoveDocument(Guid documentId)
        {
            var document = _documents.FirstOrDefault(d => d.Id == documentId && !d.IsDeleted)
                ?? throw new EntityNotFoundException(nameof(Document), documentId);
            document.Delete();
            UpdatedAt = DateTime.UtcNow;
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
