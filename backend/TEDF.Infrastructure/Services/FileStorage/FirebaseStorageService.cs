using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TEDF.Application.Common.Interfaces;
using TEDF.Infrastructure.Authentication;

namespace TEDF.Infrastructure.Services.FileStorage
{
    /// <summary>
    /// Firebase/Google Cloud Storage service implementation.
    /// </summary>
    public class FirebaseStorageService : IFileStorageService
    {
        private readonly FileStorageSettings _settings;
        private readonly ILogger<FirebaseStorageService> _logger;
        private readonly StorageClient _storageClient;

        public FirebaseStorageService(
            IOptions<FileStorageSettings> settings,
            IOptions<FirebaseSettings> firebaseSettings,
            IHostEnvironment hostEnvironment,
            ILogger<FirebaseStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var firebaseOptions = firebaseSettings.Value;
            GoogleCredential credential;

            var storageEmulatorHost = Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST");
            if (firebaseOptions.UseEmulator && !string.IsNullOrWhiteSpace(storageEmulatorHost))
            {
                // Only use fake credentials when the Cloud Storage emulator is explicitly configured.
                credential = GoogleCredential.FromAccessToken("emulator-fake-token");
            }
            else
            {
                if (firebaseOptions.UseEmulator && string.IsNullOrWhiteSpace(storageEmulatorHost))
                {
                    _logger.LogWarning(
                        "Firebase.UseEmulator=true but STORAGE_EMULATOR_HOST is not set. Falling back to real Firebase Storage credentials.");
                }

                var serviceAccountPath = ResolveServiceAccountPath(
                    firebaseOptions.ServiceAccountKeyPath,
                    hostEnvironment.ContentRootPath);

                credential = string.IsNullOrWhiteSpace(serviceAccountPath)
                    ? GoogleCredential.GetApplicationDefault()
                    : CredentialFactory.FromFile<ServiceAccountCredential>(serviceAccountPath).ToGoogleCredential();
            }

            _storageClient = StorageClient.Create(credential);
        }

        private static string ResolveServiceAccountPath(string configuredPath, string contentRootPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
            }

            // Backward-compatible fallback: keep working when key file is placed at API root.
            var fallbackPath = Path.Combine(contentRootPath, "service-account.json");
            return File.Exists(fallbackPath) ? fallbackPath : string.Empty;
        }

        /// <inheritdoc/>
        public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string folder, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.BucketName))
                {
                    return new FileUploadResult(false, null, null, "FirebaseStorage:BucketName is missing.");
                }

                // Validate file
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!_settings.AllowedExtensions.Contains(extension))
                {
                    return new FileUploadResult(false, null, null, $"File extension '{extension}' is not allowed.");
                }

                if (stream.Length > _settings.MaxFileSizeBytes)
                {
                    return new FileUploadResult(false, null, null, $"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes / (1024 * 1024)}MB.");
                }

                // Generate unique file name
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var objectName = string.IsNullOrEmpty(folder)
                    ? uniqueFileName
                    : $"{folder.TrimEnd('/')}/{uniqueFileName}";

                // Get content type
                var contentType = GetContentType(extension);

                // Upload to Firebase Storage
                var uploadObject = await _storageClient.UploadObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: objectName,
                    contentType: contentType,
                    source: stream,
                    cancellationToken: ct);

                // Make public if configured
                if (_settings.MakePublicByDefault)
                {
                    await MakeObjectPublicAsync(objectName, ct);
                }

                var publicUrl = GetPublicUrl(objectName);

                _logger.LogInformation("File uploaded successfully: {ObjectName}", objectName);

                return new FileUploadResult(true, objectName, publicUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
                return new FileUploadResult(false, null, null, $"Upload failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<FileUploadResult> MoveAsync(string sourceFilePath, string destinationFolder, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath))
                {
                    return new FileUploadResult(false, null, null, "Source file path is required.");
                }

                var destinationFileName = Path.GetFileName(sourceFilePath);
                if (string.IsNullOrWhiteSpace(destinationFileName))
                {
                    return new FileUploadResult(false, null, null, "Invalid source file path.");
                }

                var destinationObjectName = string.IsNullOrWhiteSpace(destinationFolder)
                    ? destinationFileName
                    : $"{destinationFolder.TrimEnd('/')}/{destinationFileName}";

                if (string.Equals(sourceFilePath, destinationObjectName, StringComparison.OrdinalIgnoreCase))
                {
                    return new FileUploadResult(true, sourceFilePath, GetPublicUrl(sourceFilePath), null);
                }

                await _storageClient.CopyObjectAsync(
                    sourceBucket: _settings.BucketName,
                    sourceObjectName: sourceFilePath,
                    destinationBucket: _settings.BucketName,
                    destinationObjectName: destinationObjectName,
                    cancellationToken: ct);

                if (_settings.MakePublicByDefault)
                {
                    await MakeObjectPublicAsync(destinationObjectName, ct);
                }

                await _storageClient.DeleteObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: sourceFilePath,
                    cancellationToken: ct);

                _logger.LogInformation("File moved successfully: {Source} -> {Destination}", sourceFilePath, destinationObjectName);
                return new FileUploadResult(true, destinationObjectName, GetPublicUrl(destinationObjectName), null);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Failed to move file because source was not found: {SourceFilePath}", sourceFilePath);
                return new FileUploadResult(false, null, null, "Source file not found for move operation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file: {SourceFilePath}", sourceFilePath);
                return new FileUploadResult(false, null, null, $"Move failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Stream?> DownloadAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: filePath,
                    destination: memoryStream,
                    cancellationToken: ct);

                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file: {FilePath}", filePath);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: filePath,
                    cancellationToken: ct);

                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                await _storageClient.GetObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: filePath,
                    cancellationToken: ct);

                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check file existence: {FilePath}", filePath);
                return false;
            }
        }

        /// <inheritdoc/>
        public string GetPublicUrl(string filePath)
        {
            return $"https://storage.googleapis.com/{_settings.BucketName}/{filePath}";
        }

        /// <inheritdoc/>
        public async Task<TEDF.Application.Common.Interfaces.FileInfo?> GetFileInfoAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                var obj = await _storageClient.GetObjectAsync(
                    bucket: _settings.BucketName,
                    objectName: filePath,
                    cancellationToken: ct);

                return new TEDF.Application.Common.Interfaces.FileInfo(
                    FileName: Path.GetFileName(filePath),
                    FilePath: filePath,
                    FileSize: (long)(obj.Size ?? 0),
                    ContentType: obj.ContentType ?? "application/octet-stream",
                    UploadedAt: obj.TimeCreatedDateTimeOffset?.DateTime ?? DateTime.UtcNow
                );
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file info: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Makes an object publicly accessible.
        /// </summary>
        private async Task MakeObjectPublicAsync(string objectName, CancellationToken ct = default)
        {
            try
            {
                var obj = await _storageClient.GetObjectAsync(_settings.BucketName, objectName, cancellationToken: ct);
                obj.Acl ??= new List<Google.Apis.Storage.v1.Data.ObjectAccessControl>();
                obj.Acl.Add(new Google.Apis.Storage.v1.Data.ObjectAccessControl
                {
                    Entity = "allUsers",
                    Role = "READER"
                });

                await _storageClient.UpdateObjectAsync(obj, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to make object public: {ObjectName}", objectName);
            }
        }

        /// <summary>
        /// Gets the content type for a file extension.
        /// </summary>
        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
