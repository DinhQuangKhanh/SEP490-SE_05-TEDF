using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Emails the reporter when their ticket is marked resolved.
/// </summary>
public class SendSupportTicketResolvedMailHandler : INotificationHandler<TicketResolvedEvent>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemSettingsService _settings;
    private readonly IFirestoreMailQueue _mailQueue;
    private readonly IBackgroundJobService _backgroundJobs;
    private readonly ILogger<SendSupportTicketResolvedMailHandler> _logger;

    public SendSupportTicketResolvedMailHandler(
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository,
        ISystemSettingsService settings,
        IFirestoreMailQueue mailQueue,
        IBackgroundJobService backgroundJobs,
        ILogger<SendSupportTicketResolvedMailHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _settings = settings;
        _mailQueue = mailQueue;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task Handle(TicketResolvedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _settings.GetBoolAsync(SettingKeys.EmailOnSupportTicket, true, cancellationToken)) return;

            var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
            if (ticket is null) return;

            var reporter = await _userRepository.GetByIdAsync(ticket.ReporterId, cancellationToken);
            if (reporter is null) return;

            var message = new TedfMailMessage
            {
                To = reporter.Email.Value,
                TemplateName = MailTemplateNames.SupportTicketResolved,
                // A reopened ticket can be resolved again, so the round is carried by the event id.
                DedupeKey = $"support-ticket-resolved:{notification.TicketId}:{notification.EventId}",
                Data = new Dictionary<string, string>
                {
                    ["recipientName"] = reporter.FullName,
                    ["ticketCode"] = ticket.Code.Value,
                    ["ticketTitle"] = MailFormat.Text(ticket.Title),
                    ["resolvedAt"] = MailFormat.DateTimeText(ticket.ResolvedAt ?? notification.OccurredOn),
                    ["detailUrl"] = _mailQueue.BuildDetailUrl(SupportMailRoutes.ResolveSupportPath(reporter))
                }
            };

            var messages = new List<TedfMailMessage> { message };
            _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing support-ticket-resolved email for Ticket {TicketId}", notification.TicketId);
        }
    }
}
