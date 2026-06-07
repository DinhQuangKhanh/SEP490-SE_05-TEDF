using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.InviteMember;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");

        RuleFor(x => x.StudentCode)
            .NotEmpty()
            .WithMessage("Student code is required.")
            .MaximumLength(20)
            .WithMessage("Student code must not exceed 20 characters.");

        RuleFor(x => x.Message)
            .MaximumLength(500)
            .WithMessage("Invitation message must not exceed 500 characters.");
    }
}
