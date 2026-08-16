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
    /// Emails the group leader when a student asks to join their group, so a request does not sit
    /// unanswered until the leader happens to open the app.
    /// </summary>
    public class SendGroupJoinRequestedMailHandler : INotificationHandler<JoinRequestedEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemSettingsService _settings;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendGroupJoinRequestedMailHandler> _logger;

        public SendGroupJoinRequestedMailHandler(
            IUserRepository userRepository,
            ISystemSettingsService settings,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendGroupJoinRequestedMailHandler> logger)
        {
            _userRepository = userRepository;
            _settings = settings;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(JoinRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _settings.GetBoolAsync(SettingKeys.EmailOnGroupMembership, true, cancellationToken)) return;

                // A group without a leader has nobody to address; the request still waits in-app.
                if (notification.LeaderId is not Guid leaderId) return;

                var leader = await _userRepository.GetByIdAsync(leaderId, cancellationToken);
                if (leader is null) return;

                var student = await _userRepository.GetByIdAsync(notification.StudentId, cancellationToken);

                var message = new TedfMailMessage
                {
                    To = leader.Email.Value,
                    TemplateName = MailTemplateNames.GroupJoinRequested,
                    DedupeKey = $"group-join-requested:{notification.GroupId}:{notification.StudentId}:{notification.EventId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = leader.FullName,
                        ["studentName"] = MailFormat.Text(student?.FullName, "Một sinh viên"),
                        ["groupCode"] = notification.GroupCode,
                        ["requestedAt"] = MailFormat.DateTimeText(notification.OccurredOn),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl("/student/groups")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing group-join-requested email for Group {GroupId}", notification.GroupId);
            }
        }
    }
}
