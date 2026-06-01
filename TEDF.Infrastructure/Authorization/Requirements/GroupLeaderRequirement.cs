using Microsoft.AspNetCore.Authorization;

namespace TEDF.Infrastructure.Authorization.Requirements
{
    /// <summary>
    /// Requirement for group leader authorization.
    /// </summary>
    public class GroupLeaderRequirement : IAuthorizationRequirement { }
}