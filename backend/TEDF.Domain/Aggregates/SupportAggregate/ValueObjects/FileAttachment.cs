namespace TEDF.Domain.Aggregates.SupportAggregate.ValueObjects
{
    public record FileAttachment(
        string FileName,
        string FilePath,
        long FileSize,
        string ContentType);
}
