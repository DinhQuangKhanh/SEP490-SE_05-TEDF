namespace TEDF.Application.Common;

public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse Ok(string message = "Thành công.")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, Dictionary<string, string[]>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Ok<T>(T data, string message = "Thành công.")
        => new() { Success = true, Message = message, Data = data };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}
