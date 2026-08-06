namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// One outgoing email, addressed to exactly one person. Recipients are never batched into a single
/// message so that no student or lecturer can see anybody else's address.
/// </summary>
/// <remarks>
/// Instances travel through Hangfire, so this type is deliberately a flat, JSON-round-trippable
/// shape (init-only properties, a plain dictionary) rather than a positional record.
/// </remarks>
public sealed record TedfMailMessage
{
    /// <summary>Recipient address. Written to the top-level <c>to</c> field of the Firestore document.</summary>
    public string To { get; init; } = string.Empty;

    /// <summary>A name from <see cref="MailTemplateNames"/>.</summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>Placeholder values for the template, keyed exactly as the template declares them.</summary>
    public Dictionary<string, string> Data { get; init; } = [];

    /// <summary>
    /// Stable identity of this email. Two attempts to send the same business email must produce the
    /// same key: it becomes the Firestore document id, which is what makes delivery exactly-once
    /// across API retries, replays and Hangfire re-runs.
    /// </summary>
    public string DedupeKey { get; init; } = string.Empty;
}

/// <summary>Outcome of a queue attempt, for logging.</summary>
/// <param name="Queued">Documents created — these will be delivered by the extension.</param>
/// <param name="Duplicates">Messages skipped because their document id already existed.</param>
public sealed record MailQueueResult(int Queued, int Duplicates);
