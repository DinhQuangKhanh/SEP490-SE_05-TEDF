using MediatR;
using Microsoft.AspNetCore.Mvc;
using static TEDF.API.Extensions.ApiResponseExtensions;
using TEDF.Application.Features.DirectRegistration.Commands.UpdateDirectTopic;
using TEDF.Infrastructure.Authorization.Policies;
using TEDF.API.Endpoints.Students.DirectRegistration.Requests;

namespace TEDF.API.Endpoints.Students.DirectRegistration;

public class UpdateDirectTopicEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
    app.MapPut("/api/student/direct-topic/{projectId:guid}/update", async (
            Guid projectId,
            [FromBody] UpdateDirectTopicRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
          var command = new UpdateDirectTopicCommand(
                  projectId,
                  request.NameVi,
                  request.NameEn,
                  request.NameAbbr,
                  request.Description,
                  request.Objectives,
                  request.Scope,
                  request.Technologies,
                  request.ExpectedResults,
                  request.MaxStudents
              );

          await sender.Send(command, cancellationToken);
          return NoContent("Cập nhật đề tài thành công.");
        })
        .RequireAuthorization(PolicyNames.GroupLeader)
        .WithTags("DirectRegistration")
        .WithName("UpdateDirectTopic")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
  }
}
