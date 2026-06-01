using Microsoft.AspNetCore.Authorization;

namespace TEDF.Infrastructure.Authorization.Requirements
{
    /// <summary>
    /// Requirement for same department authorization.
    /// </summary>
    public class SameDepartmentRequirement : IAuthorizationRequirement { }
}
