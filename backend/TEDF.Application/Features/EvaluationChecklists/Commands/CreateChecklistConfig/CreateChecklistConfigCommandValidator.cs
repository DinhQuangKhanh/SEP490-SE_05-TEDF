using FluentValidation;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;

public class CreateChecklistConfigCommandValidator : AbstractValidator<CreateChecklistConfigCommand>
{
    public CreateChecklistConfigCommandValidator()
    {
        RuleFor(x => x.SemesterId).GreaterThan(0).WithMessage("Học kỳ không hợp lệ.");

        RuleFor(x => x.Criteria)
            .NotNull().WithMessage("Danh sách tiêu chí không hợp lệ.")
            .Must(c => c is not null && c.Count >= 1)
            .WithMessage("Checklist phải có ít nhất 1 tiêu chí.");

        RuleForEach(x => x.Criteria).SetValidator(new ChecklistCriterionInputValidator());

        RuleFor(x => x.RequiredPassCount)
            .GreaterThan(0).WithMessage("Số tiêu chí tối thiểu cần đạt phải lớn hơn 0.")
            .LessThanOrEqualTo(x => x.Criteria == null ? 0 : x.Criteria.Count)
            .When(x => x.Criteria is not null && x.Criteria.Count >= 1)
            .WithMessage("Số tiêu chí tối thiểu cần đạt không được lớn hơn tổng số tiêu chí.");
    }
}
