namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Logging configuration settings.
    /// </summary>
    public class LoggingSettings
    {
        public string MinimumLevel { get; set; } = "Information";
        public bool EnableConsole { get; set; } = true;
        public bool EnableFile { get; set; } = true;
        public string FilePath { get; set; } = "logs/TEDF-.log";
        public bool EnableJsonFile { get; set; } = false;
        public string JsonFilePath { get; set; } = "logs/TEDF-.json";
        public int RetainedFileCountLimit { get; set; } = 30;
        public int FileSizeLimitMb { get; set; } = 100;
        public string? SeqServerUrl { get; set; }
        public string? SeqApiKey { get; set; }
    }
}
