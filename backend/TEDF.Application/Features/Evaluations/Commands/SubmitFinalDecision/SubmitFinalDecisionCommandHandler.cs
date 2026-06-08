using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitFinalDecision;

public class SubmitFinalDecisionCommandHandler : ICommandHandler<SubmitFinalDecisionCommand>
{
    private readonly IEvaluationsDomainService _evaluations;
    private readonly ICurrentUserService _currentUser;

    public SubmitFinalDecisionCommandHandler(IEvaluationsDomainService evaluations, ICurrentUserService currentUser)
    {
        _evaluations = evaluations;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SubmitFinalDecisionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _evaluations.SubmitFinalDecisionAsync(
            _currentUser.UserId.Value, request.ProjectId, request.Result, request.Notes, cancellationToken);
        return Unit.Value;
    }
}
