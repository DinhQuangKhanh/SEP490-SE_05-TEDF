using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Common.Services;

namespace TEDF.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging request execution time and details.
/// Writes structured log entries to both the console (via ILogger) and MongoDB.
/// Only commands are persisted to MongoDB — queries are logged to console only.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Computed once per generic instantiation — true only for ICommand / ICommand{T}.
    /// </summary>
    private static readonly bool IsCommand =
        typeof(ICommand).IsAssignableFrom(typeof(TRequest)) ||
        typeof(TRequest).GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRequestLogService _requestLogService;
    private readonly ActionNameResolver _actionNameResolver;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        IRequestLogService requestLogService,
        ActionNameResolver actionNameResolver)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _requestLogService = requestLogService;
        _actionNameResolver = actionNameResolver;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId    = _currentUserService.UserId?.ToString() ?? "Anonymous";
        var userEmail = _currentUserService.Email ?? "N/A";
        var userName  = _currentUserService.FullName ?? userEmail;

        _logger.LogInformation(
            "Starting request {RequestName} by User {UserId} ({UserEmail})",
            requestName, userId, userEmail);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed request {RequestName} in {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            // Log warning for slow requests (> 500ms)
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "Long running request {RequestName} ({ElapsedMilliseconds}ms) by User {UserId}",
                    requestName, stopwatch.ElapsedMilliseconds, userId);
            }

            // Only persist commands to MongoDB — queries are NOT logged
            if (IsCommand)
            {
                var actionInfo = _actionNameResolver.Resolve(requestName);

                await _requestLogService.LogAsync(
                    new RequestLogEntry(
                        RequestName:         requestName,
                        ActionDisplayName:   actionInfo.DisplayName,
                        Category:            actionInfo.Category,
                        UserId:              userId,
                        UserName:            userName,
                        UserEmail:           userEmail,
                        UserRole:            _currentUserService.Roles.FirstOrDefault() ?? "anonymous",
                        IsSuccess:           true,
                        ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                        Timestamp:           DateTime.UtcNow,
                        RequestParameters:   ExtractRequestParameters(request)),
                    cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms for User {UserId}",
                requestName, stopwatch.ElapsedMilliseconds, userId);

            // Only persist commands to MongoDB — queries are NOT logged
            if (IsCommand)
            {
                var actionInfo = _actionNameResolver.Resolve(requestName);

                await _requestLogService.LogAsync(
                    new RequestLogEntry(
                        RequestName:         requestName,
                        ActionDisplayName:   actionInfo.DisplayName,
                        Category:            actionInfo.Category,
                        UserId:              userId,
                        UserName:            userName,
                        UserEmail:           userEmail,
                        UserRole:            _currentUserService.Roles.FirstOrDefault() ?? "anonymous",
                        IsSuccess:           false,
                        ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                        Timestamp:           DateTime.UtcNow,
                        ErrorMessage:        ex.Message,
                        ErrorType:           ex.GetType().FullName,
                        StackTrace:          ex.StackTrace,
                        RequestParameters:   ExtractRequestParameters(request)),
                    cancellationToken);
            }

            throw;
        }
    }

    private static Dictionary<string, object?> ExtractRequestParameters(TRequest request)
    {
        var properties = typeof(TRequest).GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        var parameters = new Dictionary<string, object?>(properties.Length);
        foreach (var prop in properties)
        {
            try
            {
                parameters[prop.Name] = prop.GetValue(request);
            }
            catch
            {
                parameters[prop.Name] = null;
            }
        }

        return parameters;
    }
}
