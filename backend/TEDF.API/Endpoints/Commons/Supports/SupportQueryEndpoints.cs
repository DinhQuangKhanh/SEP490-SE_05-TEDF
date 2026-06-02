using MediatR;
using TEDF.API.Endpoints.Commons.Supports.Requests;
using TEDF.API.Extensions;
using TEDF.Application.Features.Supports.Queries.GetTicketById;
using TEDF.Application.Features.Supports.Queries.GetTickets;
using TEDF.Application.Features.Supports.Queries.GetTicketStats;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Commons.Supports;

public partial class SupportEndpoints : IEndpoint
{
    private static void MapQueryEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Queries: các endpoint chỉ để đọc dữ liệu, không làm thay đổi state
        // ─────────────────────────────────────────────────────────────

        #region Thống kê tổng quan ticket

        // GET /api/supports/stats
        // Trả về số liệu thống kê ticket. Admin thấy toàn hệ thống, người dùng khác chỉ thấy của mình.
        group.MapGet("stats", GetTicketStats)
            .WithName("GetTicketStats")
            .WithTags("Supports");

        #endregion

        #region Danh sách ticket (lọc, tìm kiếm)

        // GET /api/supports?searchTerm=...&status=...&priority=...
        // Admin xem tất cả ticket, người dùng khác chỉ xem ticket do mình tạo.
        group.MapGet("", GetTickets)
            .WithName("GetTickets")
            .WithTags("Supports");

        #endregion

        #region Chi tiết một ticket

        // GET /api/supports/{id}
        // Trả về thông tin chi tiết của một ticket theo id.
        group.MapGet("{id:guid}", GetTicketById)
            .WithName("GetTicketById")
            .WithTags("Supports");

        #endregion
    }

    #region Handler: thống kê tổng quan ticket

    private static async Task<IResult> GetTicketStats(ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var isAdmin = context.User.IsInRole("Admin");
        var result = await sender.Send(new GetTicketStatsQuery(reporterId, isAdmin), ct);
        return Results.Ok(result);
    }

    #endregion

    #region Handler: danh sách ticket

    private static async Task<IResult> GetTickets(
        [AsParameters] GetTicketsRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var isAdmin = context.User.IsInRole("Admin");
        var result = await sender.Send(
            new GetTicketsQuery(reporterId, isAdmin, request.SearchTerm, request.Status, request.Priority), ct);
        return Results.Ok(result);
    }

    #endregion

    #region Handler: chi tiết ticket

    private static async Task<IResult> GetTicketById(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTicketByIdQuery(id), ct);
        return Ok(result);
    }

    #endregion
}
