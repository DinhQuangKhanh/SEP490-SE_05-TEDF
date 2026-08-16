using TEDF.Application.Common.Interfaces;
using TEDF.Infrastructure.Common;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.Services.Email;

/// <summary>
/// Sends the one-off emails the Application layer asks for (today: the admin "send test email"
/// action) through the same Firestore queue every transactional email uses.
/// </summary>
/// <remarks>
/// Deliberately not an SMTP client. A test that spoke SMTP directly would prove a path nothing else
/// in the system uses; routing it through the queue means a successful test really does say the
/// production delivery path is working.
/// </remarks>
public class FirestoreEmailSender : IEmailSender
{
    private readonly IFirestoreMailQueue _mailQueue;

    public FirestoreEmailSender(IFirestoreMailQueue mailQueue)
    {
        _mailQueue = mailQueue;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var queued = await _mailQueue.EnqueueDirectAsync(to, subject, htmlBody, ct);

        // The admin is standing in front of the screen waiting for a verdict, so unlike the
        // event-driven handlers this one surfaces the failure instead of swallowing it.
        if (!queued)
            throw new EmailException(
                "Không gửi được email. Kiểm tra cấu hình FirestoreMail (Enabled, ProjectId, khoá service account) trên máy chủ.");
    }
}
