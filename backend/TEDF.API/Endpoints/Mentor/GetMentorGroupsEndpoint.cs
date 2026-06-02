using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Features.StudentGroups.DTOs;
using TEDF.Application.Features.StudentGroups.Queries.GetMentorGroups;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Mentor;

public class GetMentorGroupsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/student-groups/mentor", async (
                [FromQuery] int? semesterId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetMentorGroupsQuery(semesterId), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("StudentGroups")
            .WithName("GetMentorGroups")
            .Produces<List<MentorGroupDto>>()
            .Produces(401);
    }
}
