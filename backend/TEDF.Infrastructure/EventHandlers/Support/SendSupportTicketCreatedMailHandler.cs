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
/// Emails every admin when a support ticket is opened, so an out-of-hours request is not left
/// waiting for someone to notice the in-app badge.
/// </summary>
public class SendSupportTicketCreatedMailHandler : INotificationHandler<TicketCreatedEvent>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemSettingsService _settings;
    private readonly IFirestoreMailQueue _mailQueue;
    private readonly IBackgroundJobService _backgroundJobs;
    private readonly ILogger<SendSupportTicketCreatedMailHandler> _logger;

    public SendSupportTicketCreatedMailHandler(
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository,
        ISystemSettingsService settings,
        IFirestoreMailQueue mailQueue,
        IBackgroundJobService backgroundJobs,
        ILogger<SendSupportTicketCreatedMailHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _settings = settings;
        _mailQueue = mailQueue;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _settings.GetBoolAsync(SettingKeys.EmailOnSupportTicket, true, cancellationToken)) return;

            var ticket = await _ticketRepository.GetByIdAsync(notification.TicketId, cancellationToken);
            if (ticket is null) return;

            var admins = (await _userRepository.GetByRoleAsync(RoleNames.Admin, cancellationToken)).ToList();
            if (admins.Count == 0) return;

            var detailUrl = _mailQueue.BuildDetailUrl("/admin/support");
            var createdAt = MailFormat.DateTimeText(ticket.CreatedAt);

            var messages = admins
                .Select(admin => new TedfMailMessage
                {
                    To = admin.Email.Value,
                    TemplateName = MailTemplateNames.SupportTicketCreated,
                    DedupeKey = $"support-ticket-created:{notification.TicketId}:{admin.Id}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = admin.FullName,
                        ["ticketCode"] = notification.TicketCode,
                        ["ticketTitle"] = MailFormat.Text(ticket.Title),
                        ["category"] = MailFormat.TicketCategory(notification.Category),
                        ["priority"] = MailFormat.TicketPriority(notification.Priority),
                        ["createdAt"] = createdAt,
                        ["detailUrl"] = detailUrl
                    }
                })
                .ToList();

            _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
        }
        catch (Exception ex)
        {
            // The ticket is already committed; a mail problem must not fail the request.
            _logger.LogError(ex, "Error queueing support-ticket-created email for Ticket {TicketId}", notification.TicketId);
        }
    }
}
