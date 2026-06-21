namespace TEDF.Application.Features.StudentGroups.DTOs;

public record BulkOperationResultDto(
    int TotalRequested,
    int SuccessCount,
    List<BulkItemFailureDto> Failures,
    string Message);

public record BulkItemFailureDto(int Id, string Error);
