using FluentValidation;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;

public class SaveProjectChecklistCommandValidator : AbstractValidator<SaveProjectChecklistCommand>
{
    public SaveProjectChecklistCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Thiếu mã đề tài.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Danh sách điểm không hợp lệ.");

        // Shape-level guards only; the domain owns which criteria belong to the snapshot (entries whose
        // criterion id is not part of it are ignored) and recomputes the passed count.
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CriterionId).NotEmpty().WithMessage("Thiếu mã tiêu chí.");
            item.RuleFor(i => i.Comment)
                .MaximumLength(2000).WithMessage("Nhận xét tiêu chí không được vượt quá 2000 ký tự.");
        });

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Ghi chú không được vượt quá 2000 ký tự.");
    }
}
