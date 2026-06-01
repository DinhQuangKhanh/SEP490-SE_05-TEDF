using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TEDF.Infrastructure.Common;
using TEDF.Infrastructure.Services.Email.Templates;

namespace TEDF.Infrastructure.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<EmailSettings> settings,
            IEmailTemplateService templateService,
            ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _templateService = templateService;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            try
            {
                var email = CreateMimeMessage(message);

                using var smtp = new SmtpClient();

                var secureSocketOptions = _settings.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : (_settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);

                await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, secureSocketOptions, ct);

                if (!string.IsNullOrEmpty(_settings.Username))
                {
                    await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);
                }

                await smtp.SendAsync(email, ct);
                await smtp.DisconnectAsync(true, ct);

                _logger.LogInformation("Email sent successfully to {To}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", message.To);
                throw new EmailException($"Failed to send email to {message.To}", ex);
            }
        }

        public async Task SendTemplatedAsync<T>(string templateName, string toEmail, string subject, T model, CancellationToken ct = default)
        {
            var body = _templateService.RenderTemplate(templateName, model);
            var message = new EmailMessage(toEmail, subject, body, IsHtml: true);
            await SendAsync(message, ct);
        }

        public async Task<BulkEmailResult> SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default)
        {
            var totalSent = 0;
            var failedRecipients = new List<string>();

            foreach (var message in messages)
            {
                try
                {
                    await SendAsync(message, ct);
                    totalSent++;
                }
                catch
                {
                    failedRecipients.Add(message.To);
                }
            }

            return new BulkEmailResult(totalSent, failedRecipients.Count, failedRecipients);
        }

        private MimeMessage CreateMimeMessage(EmailMessage message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(message.To));

            if (!string.IsNullOrEmpty(message.Cc))
                email.Cc.Add(MailboxAddress.Parse(message.Cc));

            if (!string.IsNullOrEmpty(message.Bcc))
                email.Bcc.Add(MailboxAddress.Parse(message.Bcc));

            email.Subject = message.Subject;

            var builder = new BodyBuilder();
            if (message.IsHtml)
                builder.HtmlBody = message.Body;
            else
                builder.TextBody = message.Body;

            if (message.Attachments is not null)
            {
                foreach (var attachment in message.Attachments)
                {
                    builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }

            email.Body = builder.ToMessageBody();
            return email;
        }
    }
}
