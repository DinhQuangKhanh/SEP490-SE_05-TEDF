using FluentValidation;

namespace TEDF.Application.Features.StudentGroups.Commands.CreateGroup;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("Group name must not exceed 200 characters.");
    }
}
