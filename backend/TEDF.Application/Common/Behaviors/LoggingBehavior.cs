using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Common.Services;

namespace TEDF.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly bool IsCommand =
        typeof(ICommand).IsAssignableFrom(typeof(TRequest)) ||
        typeof(TRequest).GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActivityLogService _activityLogService;
    private readonly ActionNameResolver _actionNameResolver;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        IActivityLogService activityLogService,
        ActionNameResolver actionNameResolver)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _activityLogService = activityLogService;
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

                await _activityLogService.LogAsync(
                    new ActivityLogEntry(
                        ActionCode:      requestName,
                        ActionName:      actionInfo.DisplayName,
                        FeatureCategory: actionInfo.Category,
                        UserId:          userId,
                        UserName:        userName,
                        UserEmail:       userEmail,
                        Role:            _currentUserService.Roles.FirstOrDefault() ?? "anonymous",
                        IsSuccess:       true,
                        DurationMs:      stopwatch.ElapsedMilliseconds,
                        Timestamp:       DateTime.UtcNow),
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

                await _activityLogService.LogAsync(
                    new ActivityLogEntry(
                        ActionCode:      requestName,
                        ActionName:      actionInfo.DisplayName,
                        FeatureCategory: actionInfo.Category,
                        UserId:          userId,
                        UserName:        userName,
                        UserEmail:       userEmail,
                        Role:            _currentUserService.Roles.FirstOrDefault() ?? "anonymous",
                        IsSuccess:       false,
                        DurationMs:      stopwatch.ElapsedMilliseconds,
                        Timestamp:       DateTime.UtcNow),
                    cancellationToken);
            }

            throw;
        }
    }
}
