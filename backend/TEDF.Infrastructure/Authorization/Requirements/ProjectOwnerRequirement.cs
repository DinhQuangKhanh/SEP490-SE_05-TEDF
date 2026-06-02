using Microsoft.AspNetCore.Authorization;

namespace TEDF.Infrastructure.Authorization.Requirements
{
    /// <summary>
    /// Requirement for project owner authorization.
    /// </summary>
    public class ProjectOwnerRequirement : IAuthorizationRequirement { }
}
