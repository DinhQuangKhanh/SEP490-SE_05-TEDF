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
    }
}
