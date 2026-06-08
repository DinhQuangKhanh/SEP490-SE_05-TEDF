using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Users.DTOs;

namespace TEDF.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, GetUsersQueryResult>
{
    private readonly IUsersQueryService _users;

    public GetUsersQueryHandler(IUsersQueryService users) => _users = users;

    public Task<GetUsersQueryResult> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        => _users.GetUsersAsync(request.Role, request.Search, request.Page, request.PageSize, cancellationToken);
}
