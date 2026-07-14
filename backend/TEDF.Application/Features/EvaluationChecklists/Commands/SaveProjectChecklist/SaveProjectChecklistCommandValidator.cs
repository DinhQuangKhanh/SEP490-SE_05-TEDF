using FluentValidation;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

public class SaveProjectChecklistCommandValidator : AbstractValidator<SaveProjectChecklistCommand>
{
    public SaveProjectChecklistCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Thiếu mã đề tài.");

        RuleFor(x => x.PassedCriterionIds)
            .NotNull().WithMessage("Danh sách tiêu chí không hợp lệ.");

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Ghi chú không được vượt quá 2000 ký tự.");
    }
}
