using FluentValidation;

namespace TEDF.Application.Features.EvaluationChecklists.Commands.ImportChecklistConfig;

public class ImportChecklistConfigCommandValidator : AbstractValidator<ImportChecklistConfigCommand>
{
    public ImportChecklistConfigCommandValidator()
    {
        RuleFor(x => x.SemesterId).GreaterThan(0).WithMessage("Học kỳ không hợp lệ.");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("Thiếu tên file.");
        RuleFor(x => x.FileContent)
            .NotNull().Must(c => c is { Length: > 0 }).WithMessage("File import rỗng hoặc không hợp lệ.");
        RuleFor(x => x.RequiredPassCount)
            .GreaterThan(0).WithMessage("Số tiêu chí tối thiểu cần đạt phải lớn hơn 0.");
        // RequiredPassCount <= criteria count is validated by the domain once the file is parsed.
    }
}
