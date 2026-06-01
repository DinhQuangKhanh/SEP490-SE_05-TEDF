using MediatR;

namespace TEDF.Application.Common.Abstractions;

/// <summary>
/// Marker interface for commands that don't return a value.
/// Commands represent intentions to change state in the system.
/// </summary>
public interface ICommand : IRequest<Unit>
{
}

/// <summary>
/// Marker interface for commands that return a result.
/// Commands represent intentions to change state in the system.
/// </summary>
/// <typeparam name="TResponse">The type of the result.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}
