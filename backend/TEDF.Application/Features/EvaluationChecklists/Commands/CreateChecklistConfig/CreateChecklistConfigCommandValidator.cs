using FluentValidation;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;

public class CreateChecklistConfigCommandValidator : AbstractValidator<CreateChecklistConfigCommand>
{
    public CreateChecklistConfigCommandValidator()
    {
        RuleFor(x => x.SemesterId).GreaterThan(0).WithMessage("Học kỳ không hợp lệ.");

        RuleFor(x => x.Criteria)
            .NotNull().WithMessage("Danh sách tiêu chí không hợp lệ.")
            .Must(c => c is not null && c.Count >= 1 && c.Count <= ChecklistConfig.RequiredCriteriaCount)
            .WithMessage($"Checklist phải có từ 1 đến {ChecklistConfig.RequiredCriteriaCount} tiêu chí.");

        RuleForEach(x => x.Criteria).ChildRules(c =>
        {
            c.RuleFor(i => i.TitleVi).NotEmpty().WithMessage("Tên tiêu chí (tiếng Việt) không được để trống.")
                .MaximumLength(300);
            c.RuleFor(i => i.TitleEn).MaximumLength(300);
            c.RuleFor(i => i.Description).MaximumLength(2000);
        });
    }
}
