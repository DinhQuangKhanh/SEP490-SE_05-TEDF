using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that invalidates cache entries after a command succeeds.
/// Only runs for commands implementing ICacheInvalidatingCommand.
/// Clears both L1 and L2 cache for all specified prefixes.
/// Supports {userId} placeholder in prefixes (resolved at runtime from ICurrentUserService).
/// </summary>
public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidatingCommand
{
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(
        ICacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        var requestName = typeof(TRequest).Name;
        var prefixes = request.CachePrefixesToInvalidate;

        foreach (var prefix in prefixes)
        {
            var resolvedPrefix = ResolvePrefix(prefix);
            await _cacheService.RemoveByPrefixAsync(resolvedPrefix, cancellationToken);
            _logger.LogInformation(
                "Cache invalidated by {RequestName}: prefix {Prefix}",
                requestName, resolvedPrefix);
        }

        return response;
    }

    private string ResolvePrefix(string prefix)
    {
        if (prefix.Contains("{userId}"))
        {
            var userId = _currentUserService.UserId?.ToString() ?? "anonymous";
            return prefix.Replace("{userId}", userId);
        }

        return prefix;
    }
}

/// <summary>
/// MediatR pipeline behavior for commands that return a result and also invalidate cache.
/// Handles commands implementing ICacheInvalidatingCommand&lt;TResponse&gt;.
/// Supports {userId} placeholder in prefixes (resolved at runtime from ICurrentUserService).
/// </summary>
public class CacheInvalidationWithResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidatingCommand<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CacheInvalidationWithResultBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationWithResultBehavior(
        ICacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<CacheInvalidationWithResultBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        var requestName = typeof(TRequest).Name;
        var prefixes = request.CachePrefixesToInvalidate;

        foreach (var prefix in prefixes)
        {
            var resolvedPrefix = ResolvePrefix(prefix);
            await _cacheService.RemoveByPrefixAsync(resolvedPrefix, cancellationToken);
            _logger.LogInformation(
                "Cache invalidated by {RequestName}: prefix {Prefix}",
                requestName, resolvedPrefix);
        }

        return response;
    }

    private string ResolvePrefix(string prefix)
    {
        if (prefix.Contains("{userId}"))
        {
            var userId = _currentUserService.UserId?.ToString() ?? "anonymous";
            return prefix.Replace("{userId}", userId);
        }

        return prefix;
    }
}
