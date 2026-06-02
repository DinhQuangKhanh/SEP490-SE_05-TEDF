using TEDF.API.Extensions;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class GetErrorLogDetailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/error-logs/{id:guid}", async (
                IErrorLogRepository repository,
                Guid id,
                CancellationToken cancellationToken = default) =>
            {
                var errorLog = await repository.GetByIdAsync(id, cancellationToken);
                if (errorLog is null)
                    return Results.NotFound();

                return Ok(new
                {
                    errorLog.Id,
                    UserId = errorLog.UserId?.ToString(),
                    errorLog.UserName,
                    errorLog.UserEmail,
                    errorLog.ActiveRole,
                    errorLog.Severity,
                    errorLog.Source,
                    errorLog.ActionCode,
                    Action = errorLog.Action,
                    errorLog.RoutePath,
                    errorLog.RequestPath,
                    errorLog.RequestMethod,
                    errorLog.ErrorMessage,
                    errorLog.ErrorType,
                    errorLog.StackTrace,
                    InnerExceptions = errorLog.InnerExceptions.Select(ie => new
                    {
                        ie.Message,
                        ie.Type,
                        ie.StackTrace,
                    }),
                    errorLog.CorrelationId,
                    errorLog.Timestamp,
                });
            })
            .RequireAuthorization()
            .WithTags("Admin")
            .WithName("GetErrorLogDetail")
            .Produces(200)
            .Produces(401)
            .Produces(404);
    }
}
