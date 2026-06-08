using MediatR;

namespace TEDF.Application.Common.Abstractions;

/// <summary>
/// Marker interface for queries that return a result.
/// Queries are read operations that don't change state.
/// </summary>
/// <typeparam name="TResponse">The type of the result.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
