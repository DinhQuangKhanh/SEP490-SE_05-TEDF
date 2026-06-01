namespace TEDF.Infrastructure.Common;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    public static Error NotFound(string entityName, object id) =>
        new($"{entityName}.NotFound", $"{entityName} with id '{id}' was not found.");

    public static Error Validation(string propertyName, string message) =>
        new($"Validation.{propertyName}", message);

    public static Error Conflict(string message) =>
        new("Error.Conflict", message);

    public static Error Unauthorized(string message = "Unauthorized access.") =>
        new("Error.Unauthorized", message);

    public static Error Forbidden(string message = "Access is forbidden.") =>
        new("Error.Forbidden", message);

    public static Error Internal(string message = "An internal error occurred.") =>
        new("Error.Internal", message);
}