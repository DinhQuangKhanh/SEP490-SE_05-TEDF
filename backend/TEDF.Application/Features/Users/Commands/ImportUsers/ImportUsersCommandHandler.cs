using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Users.Commands.ImportUsers;

public class ImportUsersCommandHandler : ICommandHandler<ImportUsersCommand, UserImportResponse>
{
    private readonly IUsersDomainService _usersDomainService;

    public ImportUsersCommandHandler(IUsersDomainService usersDomainService) => _usersDomainService = usersDomainService;

    public async Task<UserImportResponse> Handle(ImportUsersCommand request, CancellationToken cancellationToken)
    {
        var result = await _usersDomainService.ImportUsersAsync(
            request.FileStream, request.FileName, request.ImportedBy, cancellationToken);

        return new UserImportResponse(result.TotalProcessed, result.SuccessfullyImported, result.Issues.ToList());
    }
}
