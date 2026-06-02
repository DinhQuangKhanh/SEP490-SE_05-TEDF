namespace TEDF.Infrastructure.Authentication
{
    /// <summary>
    /// Configuration settings for Firebase Authentication.
    /// </summary>
    public class FirebaseSettings
    {
        public const string SectionName = "Firebase";

        /// <summary>
        /// Firebase project ID.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Path to the service account key JSON file.
        /// </summary>
        public string ServiceAccountKeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use the Firebase emulator for local development.
        /// </summary>
        public bool UseEmulator { get; set; } = false;

        /// <summary>
        /// Firebase emulator host (e.g., "localhost:9099").
        /// </summary>
        public string? EmulatorHost { get; set; }
    }
}
