using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;

public class RespondInvitationCommandValidator : AbstractValidator<RespondInvitationCommand>
{
    public RespondInvitationCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");

        RuleFor(x => x.InvitationId)
            .GreaterThan(0)
            .WithMessage("InvitationId must be greater than 0.");
    }
}
