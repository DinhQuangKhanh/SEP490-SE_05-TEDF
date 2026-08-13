using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;
using TEDF.Persistence.SqlServer.Constants;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Emails the other party when a reply lands on a ticket: the reporter when staff answer, the
/// assignee when the reporter writes back. An unassigned ticket falls back to every admin, matching
/// <see cref="TicketMessageAddedEventHandler"/>.
/// </summary>
/// <remarks>
/// The message body is never copied into the email — only the fact that a reply exists plus a link.
/// A ticket can carry personal details, and mail leaves the system's access control behind.
/// </remarks>
public class SendSupportTicketRepliedMailHandler : INotificationHandler<TicketMessageAddedEvent>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemSettingsService _settings;
    private readonly IFirestoreMailQueue _mailQueue;
    private readonly IBackgroundJobService _backgroundJobs;
    private readonly ILogger<SendSupportTicketRepliedMailHandler> _logger;

    public SendSupportTicketRepliedMailHandler(
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository,
        ISystemSettingsService settings,
        IFirestoreMailQueue mailQueue,
        IBackgroundJobService backgroundJobs,
        ILogger<SendSupportTicketRepliedMailHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _settings = settings;
        _mailQueue = mailQueue;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task Handle(TicketMessageAddedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _settings.GetBoolAsync(SettingKeys.EmailOnSupportTicket, true, cancellationToken)) return;

            var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
            if (ticket is null) return;

            var sender = await _userRepository.GetByIdAsync(notification.SenderId, cancellationToken);
            var recipients = await ResolveRecipientsAsync(ticket, notification.SenderId, cancellationToken);
            if (recipients.Count == 0) return;

            var senderName = MailFormat.Text(sender?.FullName, "Người dùng");
            var repliedAt = MailFormat.DateTimeText(notification.OccurredOn);

            var messages = recipients
                .Select(recipient => new TedfMailMessage
                {
                    To = recipient.Email.Value,
                    TemplateName = MailTemplateNames.SupportTicketReplied,
                    // Keyed by message, so every reply is its own email while a retried dispatch of
                    // the same reply collapses onto one document.
                    DedupeKey = $"support-ticket-replied:{notification.MessageId}:{recipient.Id}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = recipient.FullName,
                        ["senderName"] = senderName,
                        ["ticketCode"] = ticket.Code.Value,
                        ["ticketTitle"] = MailFormat.Text(ticket.Title),
                        ["repliedAt"] = repliedAt,
                        ["detailUrl"] = _mailQueue.BuildDetailUrl(SupportMailRoutes.ResolveSupportPath(recipient))
                    }
                })
                .ToList();

            _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing support-ticket-replied email for Ticket {TicketId}", notification.TicketId);
        }
    }

    /// <summary>
    /// Everyone who should hear about this reply, never including its author.
    /// </summary>
    private async Task<List<TEDF.Domain.Aggregates.UserAggregate.User>> ResolveRecipientsAsync(
        SupportTicket ticket, Guid senderId, CancellationToken ct)
    {
        // The reporter wrote it → it is for the support side; anyone else wrote it → for the reporter.
        var targetId = senderId == ticket.ReporterId ? ticket.AssigneeId : ticket.ReporterId;

        if (targetId is Guid recipientId && recipientId != senderId)
        {
            var user = await _userRepository.GetByIdAsync(recipientId, ct);
            return user is null ? [] : [user];
        }

        // Nobody has picked the ticket up yet, so the whole admin desk is told.
        var admins = await _userRepository.GetByRoleAsync(RoleNames.Admin, ct);
        return admins.Where(a => a.Id != senderId).ToList();
    }
}
