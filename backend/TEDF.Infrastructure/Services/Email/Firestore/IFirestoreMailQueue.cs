namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Appends emails to the Firestore collection watched by the Trigger Email extension.
/// Implementations must be idempotent on <see cref="TedfMailMessage.DedupeKey"/>.
/// </summary>
public interface IFirestoreMailQueue
{
    Task<MailQueueResult> EnqueueAsync(IReadOnlyList<TedfMailMessage> messages, CancellationToken ct = default);

    /// <summary>
    /// Turns an in-app route (<c>/lecturer/moderate/{id}</c>) into the absolute link used by the
    /// <c>detailUrl</c> placeholder. Returns the route unchanged when no origin is configured.
    /// </summary>
    string BuildDetailUrl(string relativePath);
}
