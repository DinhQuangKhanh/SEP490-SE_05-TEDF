using FluentValidation;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists;

/// <summary>
/// Shared shape/score validation for a single checklist criterion input. The definitive enforcement still
/// happens in the domain (<c>ChecklistCriterion.Create</c>); this gives fast, field-level 400 messages.
/// </summary>
public sealed class ChecklistCriterionInputValidator : AbstractValidator<ChecklistCriterionInput>
{
    public ChecklistCriterionInputValidator()
    {
        RuleFor(i => i.TitleVi)
            .NotEmpty().WithMessage("Tên tiêu chí (tiếng Việt) không được để trống.")
            .MaximumLength(300);
        RuleFor(i => i.TitleEn).MaximumLength(300);
        RuleFor(i => i.Description).MaximumLength(2000);

        RuleFor(i => i.MaxScore)
            .GreaterThan(0).WithMessage("Điểm tối đa của tiêu chí phải lớn hơn 0.");
        RuleFor(i => i.PassScore)
            .GreaterThanOrEqualTo(0).WithMessage("Điểm đạt của tiêu chí không được âm.")
            .LessThanOrEqualTo(i => i.MaxScore).WithMessage("Điểm đạt không được lớn hơn điểm tối đa.");
    }
}
