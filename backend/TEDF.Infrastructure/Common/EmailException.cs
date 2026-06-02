namespace TEDF.Infrastructure.Common;

/// <summary>
/// Exception thrown when email sending fails.
/// </summary>
public class EmailException : InfrastructureException
{
    public EmailException(string message) : base("Email.Error", message) { }
    public EmailException(string message, Exception innerException)
        : base("Email.Error", message, innerException) { }
}