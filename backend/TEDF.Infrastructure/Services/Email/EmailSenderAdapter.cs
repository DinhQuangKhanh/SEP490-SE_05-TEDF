using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Services.Email;

/// <summary>Adapts the Application <see cref="IEmailSender"/> to the Infrastructure email service.</summary>
public class EmailSenderAdapter : IEmailSender
{
    private readonly IEmailService _emailService;

    public EmailSenderAdapter(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => _emailService.SendAsync(new EmailMessage(to, subject, htmlBody), ct);
}
