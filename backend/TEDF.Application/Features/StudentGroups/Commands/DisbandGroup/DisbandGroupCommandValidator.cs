using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.DisbandGroup;

public class DisbandGroupCommandValidator : AbstractValidator<DisbandGroupCommand>
{
    public DisbandGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithMessage("GroupId is required.");
    }
}
