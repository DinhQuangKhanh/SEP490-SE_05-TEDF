namespace TEDF.Infrastructure.Common;

/// <summary>
/// Exception thrown when an external service fails.
/// </summary>
public class ExternalServiceException : InfrastructureException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message)
        : base("ExternalService.Error", message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base("ExternalService.Error", message, innerException)
    {
        ServiceName = serviceName;
    }
}
