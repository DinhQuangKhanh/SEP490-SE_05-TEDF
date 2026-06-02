using Microsoft.AspNetCore.Authorization;

namespace TEDF.Infrastructure.Authorization.Requirements
{
    /// <summary>
    /// Requirement for mentor of project authorization.
    /// </summary>
    public class MentorOfProjectRequirement : IAuthorizationRequirement { }
}
