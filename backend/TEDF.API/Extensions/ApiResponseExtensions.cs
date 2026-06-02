using TEDF.Application.Common;

namespace TEDF.API.Extensions;

/// <summary>
/// Extension methods for consistent API response handling in Minimal APIs.
/// Wraps all responses (success/created/nocontent) in ApiResponse format.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Wraps a successful query/GET response in ApiResponse&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">Type of response data</typeparam>
    /// <param name="data">The data to return</param>
    /// <param name="message">Optional custom message (default: "Thành công.")</param>
    /// <returns>HTTP 200 with ApiResponse wrapper</returns>
    public static IResult Ok<T>(T data, string message = "Thành công.")
        => Results.Ok(ApiResponse.Ok(data, message));

    /// <summary>
    /// Wraps a successful response with only a message (no data)
    /// </summary>
    /// <param name="message">Response message</param>
    /// <returns>HTTP 200 with ApiResponse wrapper</returns>
    public static IResult Ok(string message)
        => Results.Ok(ApiResponse.Ok(message));

    /// <summary>
    /// Wraps a created resource response in ApiResponse&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">Type of created resource</typeparam>
    /// <param name="location">URI of created resource (for Location header)</param>
    /// <param name="data">The created resource data</param>
    /// <param name="message">Optional custom message</param>
    /// <returns>HTTP 201 with Location header and ApiResponse wrapper</returns>
    public static IResult Created<T>(string location, T data, string message = "Tạo mới thành công.")
        => Results.Created(location, ApiResponse.Ok(data, message));

    /// <summary>
    /// Wraps a successful operation with no content response in ApiResponse
    /// </summary>
    /// <param name="message">Optional custom message (default: "Thành công.")</param>
    /// <returns>HTTP 200 with ApiResponse wrapper (no data)</returns>
    public static IResult NoContent(string message = "Thành công.")
        => Results.Ok(ApiResponse.Ok(message));

    /// <summary>
    /// Wraps a successful operation with only a message (no data)
    /// </summary>
    /// <param name="message">Response message</param>
    /// <returns>HTTP 200 with ApiResponse wrapper</returns>
    public static IResult SuccessMessage(string message)
        => Results.Ok(ApiResponse.Ok(message));
}
