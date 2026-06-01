namespace TEDF.Infrastructure.Common;

/// <summary>
/// Exception thrown when file operation fails.
/// </summary>
public class FileStorageException : InfrastructureException
{
    public FileStorageException(string message) : base("FileStorage.Error", message) { }
    public FileStorageException(string message, Exception innerException)
        : base("FileStorage.Error", message, innerException) { }
}