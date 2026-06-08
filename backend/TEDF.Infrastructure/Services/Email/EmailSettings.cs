
namespace TEDF.Infrastructure.Services.Email
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "TEDF System";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
        public bool UseStartTls { get; set; } = true;
    }

    public record EmailMessage(
        string To,
        string Subject,
        string Body,
        bool IsHtml = true,
        string? Cc = null,
        string? Bcc = null,
        IEnumerable<EmailAttachment>? Attachments = null
    );

    public record EmailAttachment(string FileName, byte[] Content, string ContentType);

    public record BulkEmailResult(int TotalSent, int TotalFailed, List<string> FailedRecipients);
}
