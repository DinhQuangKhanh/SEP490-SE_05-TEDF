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
    /// Emails the invited student when a group leader invites them.
    /// Runs alongside <see cref="MemberInvitedEventHandler"/>, which raises the in-app notification.
    /// </summary>
    public class SendGroupInvitationMailHandler : INotificationHandler<MemberInvitedEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemSettingsService _settings;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendGroupInvitationMailHandler> _logger;

        public SendGroupInvitationMailHandler(
            IUserRepository userRepository,
            ISystemSettingsService settings,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendGroupInvitationMailHandler> logger)
        {
            _userRepository = userRepository;
            _settings = settings;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(MemberInvitedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _settings.GetBoolAsync(SettingKeys.EmailOnGroupMembership, true, cancellationToken)) return;

                var invitee = await _userRepository.GetByIdAsync(notification.InviteeId, cancellationToken);
                if (invitee is null) return;

                var inviter = await _userRepository.GetByIdAsync(notification.InviterId, cancellationToken);

                var message = new TedfMailMessage
                {
                    To = invitee.Email.Value,
                    TemplateName = MailTemplateNames.GroupInvitation,
                    // A student may be invited again after declining, so the key carries the event
                    // id: a genuine second invitation is a new email, while a retried dispatch of
                    // the same event still collapses onto one document.
                    DedupeKey = $"group-invitation:{notification.GroupId}:{notification.InviteeId}:{notification.EventId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = invitee.FullName,
                        ["inviterName"] = MailFormat.Text(inviter?.FullName, "Một sinh viên"),
                        ["groupCode"] = notification.GroupCode,
                        ["invitedAt"] = MailFormat.DateTimeText(notification.OccurredOn),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl("/student/groups/invitations")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                // The invitation is already committed; a mail problem must not fail the request.
                _logger.LogError(ex, "Error queueing group-invitation email for Group {GroupId}", notification.GroupId);
            }
        }
    }
}
