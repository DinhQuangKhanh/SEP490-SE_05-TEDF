using MediatR;
using TEDF.Application.Features.Authentications.Queries.GetSession;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Authentications;

public sealed class AuthenticationsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").RequireAuthorization();

        // Session bootstrap: the SPA calls this once after Firebase sign-in to learn its access state.
        group.MapGet("/session", GetSession)
            .WithTags("Auth")
            .WithName("GetSession")
            .Produces(200)
            .Produces(401);
    }

    private static async Task<IResult> GetSession(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetSessionQuery(), ct));
}
