using TEDF.Application.Features.Users.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Users feature.
/// Query handlers in <c>Application/Features/Users</c> depend on this service only.
/// </summary>
public interface IUsersQueryService
{
    Task<GetUsersQueryResult> GetUsersAsync(
        string? role,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MyProfileDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
