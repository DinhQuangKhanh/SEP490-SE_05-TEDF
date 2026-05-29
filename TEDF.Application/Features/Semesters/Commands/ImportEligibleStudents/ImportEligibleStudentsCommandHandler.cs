using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;

public class ImportEligibleStudentsCommandHandler : ICommandHandler<ImportEligibleStudentsCommand, ImportEligibleStudentsResponse>
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IExcelService _excelService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImportEligibleStudentsCommandHandler> _logger;

    public ImportEligibleStudentsCommandHandler(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        IExcelService excelService,
        IUnitOfWork unitOfWork,
        ILogger<ImportEligibleStudentsCommandHandler> logger)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _excelService = excelService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ImportEligibleStudentsResponse> Handle(ImportEligibleStudentsCommand request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(request.SemesterId, cancellationToken);
        if (semester == null)
        {
            throw new EntityNotFoundException(nameof(Semester), request.SemesterId);
        }

        if (request.FileStream == null || request.FileStream.Length == 0)
        {
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");
        }

        var studentCodes = await _excelService.ExtractStudentCodesAsync(request.FileStream, request.FileName, cancellationToken);

        if (!studentCodes.Any())
        {
            throw new BusinessRuleValidationException("Không tìm thấy mã sinh viên nào trong file. Đảm bảo file có cột chứa mã sinh viên hợp lệ.");
        }

        int successCount = 0;
        var failedCodes = new List<string>();

        foreach (var code in studentCodes)
        {
            var student = await _userRepository.GetByStudentCodeAsync(code, cancellationToken);
            if (student != null)
            {
                semester.AddEligibleStudent(student.Id, code, request.ImportedBy);
                successCount++;
            }
            else
            {
                failedCodes.Add(code);
            }
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported {Count} eligible students for Semester {SemesterId}. Failed: {FailedCount}", successCount, request.SemesterId, failedCodes.Count);

        return new ImportEligibleStudentsResponse(studentCodes.Count, successCount, failedCodes);
    }
}
