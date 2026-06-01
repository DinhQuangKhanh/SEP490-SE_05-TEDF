using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Mentor.DTOs;
using TEDF.Application.Features.Mentor.Queries.GetMentorTopics;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor;

public class GetMentorTopicsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mentor/topics", async (
                [FromQuery] int? semesterId,
                [FromQuery] string? search,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                var result = await sender.Send(
                    new GetMentorTopicsQuery(semesterId, search, page, pageSize),
                    cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("Mentor")
            .WithName("GetMentorTopics")
            .Produces<ApiResponse<GetMentorTopicsResult>>()
            .Produces(401);
    }
}
