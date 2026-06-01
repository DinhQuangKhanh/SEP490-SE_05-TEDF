using MediatR;

namespace TEDF.Application.Common.Abstractions;

/// <summary>
/// Handler interface for queries that return a result.
/// </summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResponse">The type of the result.</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
