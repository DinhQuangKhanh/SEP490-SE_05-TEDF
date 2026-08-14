using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.LeaveGroup;

public class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");
    }
}
