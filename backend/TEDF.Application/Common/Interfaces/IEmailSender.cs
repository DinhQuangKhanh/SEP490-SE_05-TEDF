namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Application-layer abstraction over the Infrastructure email service, so command handlers can
/// send mail without depending on Infrastructure. The implementation queues the message for the
/// Firestore Trigger Email extension; SMTP credentials never enter this process.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
