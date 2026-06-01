namespace TEDF.Infrastructure.Services.FileStorage
{
    /// <summary>
    /// Configuration settings for Firebase Storage.
    /// </summary>
    public class FileStorageSettings
    {
        public const string SectionName = "FirebaseStorage";

        /// <summary>
        /// Firebase Storage bucket name (e.g., "your-project.appspot.com").
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Maximum allowed file size in bytes. Default is 50MB.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for upload.
        /// </summary>
        public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".zip"];

        /// <summary>
        /// Token expiration time for signed URLs in minutes. Default is 60 minutes.
        /// </summary>
        public int SignedUrlExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to make uploaded files public by default.
        /// </summary>
        public bool MakePublicByDefault { get; set; } = true;
    }
}
