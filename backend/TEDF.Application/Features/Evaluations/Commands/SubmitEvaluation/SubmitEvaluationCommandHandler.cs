using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitEvaluation;

public class SubmitEvaluationCommandHandler : ICommandHandler<SubmitEvaluationCommand>
{
    private readonly IEvaluationsDomainService _evaluations;
    private readonly ICurrentUserService _currentUser;

    public SubmitEvaluationCommandHandler(IEvaluationsDomainService evaluations, ICurrentUserService currentUser)
    {
        _evaluations = evaluations;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _evaluations.SubmitEvaluationAsync(
            _currentUser.UserId.Value, request.ProjectId, request.Result, request.Feedback, cancellationToken);
        return Unit.Value;
    }
}
