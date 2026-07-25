using FluentValidation;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;

public class UpdateChecklistConfigCommandValidator : AbstractValidator<UpdateChecklistConfigCommand>
{
    public UpdateChecklistConfigCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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
