using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Queries.ValidateRegisterForm;

/// <summary>Runs the shared parse + b/a/c validation via the domain service and maps to the preview DTO.</summary>
public class ValidateRegisterFormQueryHandler : IQueryHandler<ValidateRegisterFormQuery, RegisterFormPreviewDto>
{
    private readonly ITopicProposalService _topicPools;
    private readonly ICurrentUserService _currentUser;

    public ValidateRegisterFormQueryHandler(ITopicProposalService topicPools, ICurrentUserService currentUser)
    {
        _topicPools = topicPools;
        _currentUser = currentUser;
    }

    public async Task<RegisterFormPreviewDto> Handle(ValidateRegisterFormQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var r = await _topicPools.ValidateRegisterFormAsync(
            request.PoolId, request.RegisterForm, _currentUser.UserId.Value, cancellationToken);

        return new RegisterFormPreviewDto(
            NameEn: r.NameEn,
            NameVi: r.NameVi,
            NameAbbr: r.NameAbbr,
            Description: r.Description,
            Objectives: r.Objectives,
            Technologies: r.Technologies,
            ExpectedResults: r.ExpectedResults,
            Scope: r.Scope,
            MentorCount: r.MentorIds.Count);
    }
}
