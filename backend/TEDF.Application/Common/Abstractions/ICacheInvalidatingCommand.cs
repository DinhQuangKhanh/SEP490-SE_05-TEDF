namespace TEDF.Application.Common.Abstractions;

/// <summary>
/// Marker interface for commands that should invalidate cache entries after successful execution.
/// The CacheInvalidationBehavior will clear all specified prefixes from both L1 and L2 cache.
/// </summary>
public interface ICacheInvalidatingCommand : ICommand
{
    /// <summary>
    /// Cache key prefixes to invalidate from both L1 and L2 after command succeeds.
    /// Example: ["evaluator:{evaluatorId}:", "evaluator:filter-options"]
    /// </summary>
    IReadOnlyCollection<string> CachePrefixesToInvalidate { get; }
}

/// <summary>
/// Generic variant for commands that return a result and should also invalidate cache entries.
/// </summary>
/// <typeparam name="TResponse">The type of the command result.</typeparam>
public interface ICacheInvalidatingCommand<TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// Cache key prefixes to invalidate from both L1 and L2 after command succeeds.
    /// </summary>
    IReadOnlyCollection<string> CachePrefixesToInvalidate { get; }
}
