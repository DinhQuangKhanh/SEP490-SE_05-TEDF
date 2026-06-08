
namespace TEDF.Infrastructure.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(EmailMessage message, CancellationToken ct = default);
        Task SendTemplatedAsync<T>(string templateName, string toEmail, string subject, T model, CancellationToken ct = default);
        Task<BulkEmailResult> SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default);
    }
}
