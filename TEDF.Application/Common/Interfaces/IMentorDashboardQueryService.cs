using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Common.Interfaces;

public interface IMentorDashboardQueryService
{
    Task<MentorDashboardDto> GetDashboardAsync(Guid mentorId, CancellationToken cancellationToken = default);
}
