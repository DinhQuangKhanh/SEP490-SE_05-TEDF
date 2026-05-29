using Microsoft.AspNetCore.Authorization;

namespace TEDF.Infrastructure.Authorization.Requirements
{
  /// <summary>
  /// Requirement for department head of a specific department authorization.
  /// </summary>
  public class DepartmentHeadOfDepartmentRequirement : IAuthorizationRequirement { }
}
