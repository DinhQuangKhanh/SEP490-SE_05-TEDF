namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Appends emails to the Firestore collection watched by the Trigger Email extension.
/// Implementations must be idempotent on <see cref="TedfMailMessage.DedupeKey"/>.
/// </summary>
public interface IFirestoreMailQueue
{
    Task<MailQueueResult> EnqueueAsync(IReadOnlyList<TedfMailMessage> messages, CancellationToken ct = default);

    /// <summary>
    /// Queues a one-off email whose subject and body are supplied inline instead of naming a
    /// template. Used by the admin "send test email" action, which has to prove the delivery path
    /// works without depending on a template document existing.
    /// </summary>
    /// <returns>False when mail is disabled or Firestore cannot be reached.</returns>
    Task<bool> EnqueueDirectAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>
    /// Turns an in-app route (<c>/lecturer/moderate/{id}</c>) into the absolute link used by the
    /// <c>detailUrl</c> placeholder. Returns the route unchanged when no origin is configured.
    /// </summary>
    string BuildDetailUrl(string relativePath);
}
