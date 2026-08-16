using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TEDF.Infrastructure.Authentication;

namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Writes one document per recipient into the Firestore <c>mail</c> collection; the
/// <c>firebase/firestore-send-email</c> extension picks them up, renders the template from
/// <c>emailTemplates</c> and performs the SMTP delivery. The extension owns the <c>delivery</c>
/// field — this class never writes it.
/// </summary>
/// <remarks>
/// <para>
/// Exactly-once delivery comes from the document id: it is derived from
/// <see cref="TedfMailMessage.DedupeKey"/> and created with <c>CreateAsync</c>, which fails with
/// <see cref="StatusCode.AlreadyExists"/> when the same business email was queued before. A retried
/// API call, a replayed domain event or a Hangfire re-run therefore cannot produce a second email.
/// </para>
/// <para>
/// Firestore is always reached with real service-account credentials. <c>Firebase:UseEmulator</c>
/// configures the <b>Authentication</b> emulator only (port 9099) and must never be applied here,
/// or a local run would silently write nowhere.
/// </para>
/// </remarks>
public sealed class FirestoreMailQueue : IFirestoreMailQueue
{
    /// <summary>Cap on concurrent Firestore writes so a large roster cannot saturate the job worker.</summary>
    private const int MaxConcurrentWrites = 5;

    /// <summary>Firestore rejects ids over 1500 bytes; our keys are far shorter, this is a guard rail.</summary>
    private const int MaxDocumentIdLength = 500;

    /// <summary>Collection the extension is installed against.</summary>
    private const string DefaultMailCollection = "mail";

    private readonly FirestoreMailOptions _options;
    private readonly FirebaseSettings _firebaseSettings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<FirestoreMailQueue> _logger;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private FirestoreDb? _db;

    public FirestoreMailQueue(
        IOptions<FirestoreMailOptions> options,
        IOptions<FirebaseSettings> firebaseSettings,
        IHostEnvironment hostEnvironment,
        ILogger<FirestoreMailQueue> logger)
    {
        _options = options.Value;
        _firebaseSettings = firebaseSettings.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<MailQueueResult> EnqueueAsync(IReadOnlyList<TedfMailMessage> messages, CancellationToken ct = default)
    {
        var deliverable = messages
            .Where(IsDeliverable)
            .GroupBy(m => m.DedupeKey, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        if (deliverable.Count == 0) return new MailQueueResult(0, 0);

        var db = await GetDatabaseAsync(ct);
        if (db is null) return new MailQueueResult(0, 0);

        var collectionName = string.IsNullOrWhiteSpace(_options.MailCollection)
            ? DefaultMailCollection
            : _options.MailCollection.Trim();

        var collection = db.Collection(collectionName);
        var queued = 0;
        var duplicates = 0;

        using var throttle = new SemaphoreSlim(MaxConcurrentWrites, MaxConcurrentWrites);
        var writes = deliverable.Select(async message =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                var created = await CreateMailDocumentAsync(collection, message, ct);
                if (created) Interlocked.Increment(ref queued);
                else Interlocked.Increment(ref duplicates);
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(writes);

        _logger.LogInformation(
            "Queued {Queued} mail document(s) in '{Collection}' ({Duplicates} already existed).",
            queued, collectionName, duplicates);

        return new MailQueueResult(queued, duplicates);
    }

    public async Task<bool> EnqueueDirectAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@')) return false;

        var db = await GetDatabaseAsync(ct);
        if (db is null) return false;

        var collectionName = string.IsNullOrWhiteSpace(_options.MailCollection)
            ? DefaultMailCollection
            : _options.MailCollection.Trim();

        // `message` is the extension's inline alternative to `template`. No dedupe key: every click
        // of "send test email" is meant to produce a new email, so the id is random.
        var document = new Dictionary<string, object>
        {
            ["to"] = to,
            ["message"] = new Dictionary<string, object>
            {
                ["subject"] = subject,
                ["html"] = htmlBody
            },
            ["tedf"] = new Dictionary<string, object>
            {
                ["dedupeKey"] = string.Empty,
                ["queuedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            }
        };

        await db.Collection(collectionName).Document($"system-test-{Guid.NewGuid():N}").CreateAsync(document, ct);
        _logger.LogInformation("Queued a test email in '{Collection}'.", collectionName);
        return true;
    }

    public string BuildDetailUrl(string relativePath)
    {
        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        var origin = _options.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrEmpty(origin) ? path : origin + path;
    }

    /// <summary>
    /// Creates the document. Returns false when the id is already taken, which means this exact
    /// email was queued by an earlier attempt and must not be sent again.
    /// </summary>
    private async Task<bool> CreateMailDocumentAsync(CollectionReference collection, TedfMailMessage message, CancellationToken ct)
    {
        // `to` sits at the top level, as a sibling of `template` — the extension resolves recipients
        // from there and reports "No recipients defined" if it is nested inside template.data.
        // Nested maps are built as Dictionary<string, object> because that is the shape the Firestore
        // serializer treats as a document map.
        var document = new Dictionary<string, object>
        {
            ["to"] = message.To,
            ["template"] = new Dictionary<string, object>
            {
                ["name"] = message.TemplateName,
                ["data"] = message.Data.ToDictionary(entry => entry.Key, entry => (object)entry.Value)
            },
            // Tracing metadata; ignored by the extension.
            ["tedf"] = new Dictionary<string, object>
            {
                ["dedupeKey"] = message.DedupeKey,
                ["queuedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
            }
        };

        try
        {
            await collection.Document(ToDocumentId(message.DedupeKey)).CreateAsync(document, ct);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            _logger.LogInformation(
                "Skipped duplicate mail for template {Template} (dedupe key {DedupeKey}).",
                message.TemplateName, message.DedupeKey);
            return false;
        }
    }

    private bool IsDeliverable(TedfMailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.DedupeKey))
        {
            _logger.LogWarning("Dropped a {Template} mail with no dedupe key.", message.TemplateName);
            return false;
        }

        // A missing address is normal for accounts imported without contact details — it is a data
        // gap, not a fault, so it is logged without the address itself.
        if (string.IsNullOrWhiteSpace(message.To) || !message.To.Contains('@'))
        {
            _logger.LogWarning(
                "Skipped {Template} mail (dedupe key {DedupeKey}): recipient has no usable email address.",
                message.TemplateName, message.DedupeKey);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds the Firestore client on first use. Returns null when mail is switched off or no project
    /// id is configured, so a misconfigured environment degrades to "no email" instead of failing
    /// the business operation that triggered it.
    /// </summary>
    private async Task<FirestoreDb?> GetDatabaseAsync(CancellationToken ct)
    {
        if (_db is not null) return _db;

        if (!_options.Enabled)
        {
            _logger.LogWarning("FirestoreMail is disabled; no email will be queued.");
            return null;
        }

        var projectId = string.IsNullOrWhiteSpace(_options.ProjectId)
            ? _firebaseSettings.ProjectId
            : _options.ProjectId;

        if (string.IsNullOrWhiteSpace(projectId))
        {
            _logger.LogWarning(
                "Neither {MailSection}:ProjectId nor {FirebaseSection}:ProjectId is configured; no email will be queued.",
                FirestoreMailOptions.SectionName, FirebaseSettings.SectionName);
            return null;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            _db ??= new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = BuildCredential()
            }.Build();

            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Resolves the credential used to reach Firestore. A configured key file wins; when it is not
    /// on disk the process falls back to application-default credentials (e.g. a developer who ran
    /// <c>gcloud auth application-default login</c>) rather than failing with a bare
    /// <see cref="FileNotFoundException"/> from inside a background job.
    /// </summary>
    private GoogleCredential BuildCredential()
    {
        var configuredPath = string.IsNullOrWhiteSpace(_options.ServiceAccountKeyPath)
            ? _firebaseSettings.ServiceAccountKeyPath
            : _options.ServiceAccountKeyPath;

        var serviceAccountPath = ResolveServiceAccountPath(configuredPath);

        if (string.IsNullOrWhiteSpace(serviceAccountPath))
            return GoogleCredential.GetApplicationDefault();

        if (!File.Exists(serviceAccountPath))
        {
            // The path is logged, never the file contents.
            _logger.LogWarning(
                "Service-account key '{Path}' was not found; falling back to application-default credentials for Firestore mail.",
                serviceAccountPath);
            return GoogleCredential.GetApplicationDefault();
        }

        return CredentialFactory.FromFile<ServiceAccountCredential>(serviceAccountPath).ToGoogleCredential();
    }

    private string ResolveServiceAccountPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return string.Empty;

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, configuredPath));
    }

    /// <summary>
    /// Maps a dedupe key onto a legal Firestore document id: no slashes, not a reserved
    /// <c>__name__</c>-style id, and short enough to stay well inside the 1500-byte limit.
    /// </summary>
    private static string ToDocumentId(string dedupeKey)
    {
        var id = dedupeKey.Replace('/', '_').Replace('\\', '_');
        if (id.Length > MaxDocumentIdLength) id = id[..MaxDocumentIdLength];
        return id is "." or ".." ? "_" + id : id;
    }
}
