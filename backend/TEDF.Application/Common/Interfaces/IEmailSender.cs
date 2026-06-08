namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Application-layer abstraction over the Infrastructure email service, so command handlers can
/// send mail without depending on Infrastructure. Credentials stay server-side in EmailSettings.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
