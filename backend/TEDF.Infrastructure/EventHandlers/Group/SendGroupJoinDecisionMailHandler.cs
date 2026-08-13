using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    /// <summary>
    /// Emails the student the outcome of their join request. Approval and rejection share one
    /// template with a <c>decision</c> placeholder, the same shape as <c>topic-final-decision</c>.
    /// </summary>
    public class SendGroupJoinDecisionMailHandler :
        INotificationHandler<JoinRequestApprovedEvent>,
        INotificationHandler<JoinRequestRejectedEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemSettingsService _settings;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendGroupJoinDecisionMailHandler> _logger;

        public SendGroupJoinDecisionMailHandler(
            IUserRepository userRepository,
            ISystemSettingsService settings,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendGroupJoinDecisionMailHandler> logger)
        {
            _userRepository = userRepository;
            _settings = settings;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public Task Handle(JoinRequestApprovedEvent notification, CancellationToken cancellationToken) =>
            SendAsync(notification.GroupId, notification.GroupCode, notification.StudentId,
                notification.EventId, notification.OccurredOn, "Được chấp nhận", cancellationToken);

        public Task Handle(JoinRequestRejectedEvent notification, CancellationToken cancellationToken) =>
            SendAsync(notification.GroupId, notification.GroupCode, notification.StudentId,
                notification.EventId, notification.OccurredOn, "Bị từ chối", cancellationToken);

        private async Task SendAsync(
            Guid groupId, string groupCode, Guid studentId, Guid eventId,
            DateTime occurredOn, string decision, CancellationToken ct)
        {
            try
            {
                if (!await _settings.GetBoolAsync(SettingKeys.EmailOnGroupMembership, true, ct)) return;

                var student = await _userRepository.GetByIdAsync(studentId, ct);
                if (student is null) return;

                var message = new TedfMailMessage
                {
                    To = student.Email.Value,
                    TemplateName = MailTemplateNames.GroupJoinDecision,
                    // A student may request the same group again after a rejection, so the event id
                    // keeps the two decisions apart while a retried dispatch stays idempotent.
                    DedupeKey = $"group-join-decision:{groupId}:{studentId}:{eventId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = student.FullName,
                        ["groupCode"] = groupCode,
                        ["decision"] = decision,
                        ["decidedAt"] = MailFormat.DateTimeText(occurredOn),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl("/student/groups")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing group-join-decision email for Group {GroupId}", groupId);
            }
        }
    }
}
