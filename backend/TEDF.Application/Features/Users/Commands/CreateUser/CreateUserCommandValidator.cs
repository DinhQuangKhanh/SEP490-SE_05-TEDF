using FluentValidation;

namespace TEDF.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Role).NotEmpty().WithMessage("Vai trò không được để trống.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email không hợp lệ.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage("Mã số không hợp lệ.");
        RuleFor(x => x.AcademicTitle).MaximumLength(100);
    }
}
