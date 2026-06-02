using MediatR;
using TEDF.Application.Features.DirectRegistration.Queries.GetAvailableMentors;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Students.DirectRegistration;

public partial class DirectRegistrationEndpoints : IEndpoint
{
    private static void MapQueryEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Queries: các endpoint chỉ để đọc dữ liệu, không làm thay đổi state
        // ─────────────────────────────────────────────────────────────

        #region Lấy danh sách giảng viên hướng dẫn khả dụng

        // GET /api/student/available-mentors?majorId=...
        // Trả về danh sách giảng viên có thể hướng dẫn, lọc theo ngành (nếu có).
        group.MapGet("available-mentors", GetAvailableMentors)
            .WithName("GetAvailableMentors")
            .WithTags("DirectRegistration")
            .Produces<List<AvailableMentorDto>>()
            .Produces(401);

        #endregion
    }

    #region Handler: lấy danh sách giảng viên khả dụng

    private static async Task<IResult> GetAvailableMentors(
        int? majorId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAvailableMentorsQuery(majorId), cancellationToken);
        return Ok(result);
    }

    #endregion
}
