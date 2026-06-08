namespace TEDF.Infrastructure.Common;

/// <summary>
/// Base exception for infrastructure layer.
/// </summary>
public class InfrastructureException : Exception
{
    public string Code { get; }

    public InfrastructureException(string code, string message) : base(message)
    {
        Code = code;
    }

    public InfrastructureException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}