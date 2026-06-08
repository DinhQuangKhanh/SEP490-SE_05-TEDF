using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Evaluations.Commands.AssignEvaluator;

/// <summary>Department head assigns an evaluator; delegates to <see cref="IEvaluationsDomainService"/>.</summary>
public class AssignEvaluatorCommandHandler : ICommandHandler<AssignEvaluatorCommand>
{
    private readonly IEvaluationsDomainService _evaluations;
    private readonly ICurrentUserService _currentUser;

    public AssignEvaluatorCommandHandler(IEvaluationsDomainService evaluations, ICurrentUserService currentUser)
    {
        _evaluations = evaluations;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AssignEvaluatorCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _evaluations.AssignEvaluatorAsync(
            _currentUser.UserId.Value, request.ProjectId, request.EvaluatorId, request.EvaluatorOrder, cancellationToken);
        return Unit.Value;
    }
}
