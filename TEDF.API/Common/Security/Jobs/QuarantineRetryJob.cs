using UniThesis.API.Common.Security.Jobs;
using UniThesis.Application.Common.Interfaces;
using UniThesis.Persistence.MongoDB.Repositories.Interfaces;

namespace UniThesis.API.Common.Security.Jobs;

/// <summary>
/// Recurring job that finds files stuck in quarantine and re-enqueues their malware scan.
/// A file is considered "stuck" if its quarantine tracking record is older than <see cref="StaleThreshold"/>
/// without being promoted — e.g. because the Hangfire job failed all retries or was lost on server restart.
/// </summary>
public sealed class QuarantineRetryJob
{
  /// <summary>Files queued more than this long ago without completing are considered stuck.</summary>
  private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

  private readonly IQuarantinedAttachmentRepository _quarantineTracking;
  private readonly IBackgroundJobService _backgroundJobService;
  private readonly ILogger<QuarantineRetryJob> _logger;

  public QuarantineRetryJob(
      IQuarantinedAttachmentRepository quarantineTracking,
      IBackgroundJobService backgroundJobService,
      ILogger<QuarantineRetryJob> logger)
  {
    _quarantineTracking = quarantineTracking;
    _backgroundJobService = backgroundJobService;
    _logger = logger;
  }

  public async Task ExecuteAsync()
  {
    var stale = await _quarantineTracking.GetStaleAsync(StaleThreshold);

    if (stale.Count == 0)
    {
      _logger.LogDebug("QuarantineRetryJob: no stuck quarantine files found.");
      return;
    }

    _logger.LogWarning(
        "QuarantineRetryJob: found {Count} stuck quarantine file(s). Re-enqueuing scan jobs.",
        stale.Count);

    foreach (var entry in stale)
    {
      _logger.LogInformation(
          "Re-enqueuing scan for stuck quarantine file: {QuarantinePath} (queued at {QueuedAt:u})",
          entry.QuarantinePath, entry.QueuedAt);

      // Re-use same parameters — AttachmentScanJob is idempotent (checks alreadyMapped)
      _backgroundJobService.Enqueue<AttachmentScanJob>(
          job => job.ExecuteAsync(
              entry.FolderPrefix,
              entry.ProjectId,
              entry.UploadedBy,
              entry.DocumentTypeInt,
              entry.FolderPartitionId,
              entry.QuarantinePath,
              entry.OriginalFileName));

      // Refresh the QueuedAt timestamp so we don't re-enqueue again on the next run
      // before this new job has had a chance to execute.
      // We do this by deleting and re-inserting with updated time.
      await _quarantineTracking.DeleteByQuarantinePathAsync(entry.QuarantinePath);
      entry.QueuedAt = DateTime.UtcNow;
      await _quarantineTracking.AddAsync(entry);
    }
  }
}