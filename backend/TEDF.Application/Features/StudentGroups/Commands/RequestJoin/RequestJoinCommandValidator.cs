using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.RequestJoin;

public class RequestJoinCommandValidator : AbstractValidator<RequestJoinCommand>
{
    public RequestJoinCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");

        RuleFor(x => x.Message)
            .MaximumLength(500)
            .WithMessage("Join request message must not exceed 500 characters.");
    }
}
