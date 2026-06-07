using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;

public class RespondJoinRequestCommandValidator : AbstractValidator<RespondJoinRequestCommand>
{
    public RespondJoinRequestCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");

        RuleFor(x => x.RequestId)
            .GreaterThan(0)
            .WithMessage("RequestId must be greater than 0.");
    }
}
