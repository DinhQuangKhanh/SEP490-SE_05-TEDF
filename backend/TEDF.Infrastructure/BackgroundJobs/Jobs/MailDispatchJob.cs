using Microsoft.Extensions.Logging;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    /// <summary>
    /// Delivers a batch of already-composed emails to the Firestore mail collection.
    /// </summary>
    /// <remarks>
    /// Domain-event handlers compose the messages inside the request (where the data is loaded and
    /// consistent) and hand them to this job, so no network call to Firebase happens on the request
    /// thread and a Firebase outage cannot affect the business operation that just committed.
    /// Failures propagate on purpose: Hangfire retries the job, and the deterministic document ids
    /// make every retry idempotent.
    /// </remarks>
    public class MailDispatchJob
    {
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly ILogger<MailDispatchJob> _logger;

        public MailDispatchJob(IFirestoreMailQueue mailQueue, ILogger<MailDispatchJob> logger)
        {
            _mailQueue = mailQueue;
            _logger = logger;
        }

        public async Task ExecuteAsync(List<TedfMailMessage> messages)
        {
            if (messages is null || messages.Count == 0) return;

            var result = await _mailQueue.EnqueueAsync(messages);

            _logger.LogInformation(
                "Mail dispatch for template(s) {Templates}: {Queued} queued, {Duplicates} duplicate(s).",
                string.Join(", ", messages.Select(m => m.TemplateName).Distinct()),
                result.Queued, result.Duplicates);
        }
    }
}
