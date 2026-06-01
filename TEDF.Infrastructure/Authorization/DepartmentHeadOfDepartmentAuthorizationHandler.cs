using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Infrastructure.Authorization.Requirements;
using TEDF.Domain.Entities;
using AppClaimTypes = TEDF.Application.Common.AppClaimTypes;

namespace TEDF.Infrastructure.Authorization
{
  /// <summary>
  /// Handles department head of department authorization.
  /// Validates that the user is the head of their assigned department.
  /// </summary>
  public class DepartmentHeadOfDepartmentAuthorizationHandler : AuthorizationHandler<DepartmentHeadOfDepartmentRequirement>
  {
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentHeadOfDepartmentAuthorizationHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository)
    {
      _userRepository = userRepository;
      _departmentRepository = departmentRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentHeadOfDepartmentRequirement requirement)
    {
      if (context.User.IsInRole("Admin"))
      {
        context.Succeed(requirement);
        return;
      }

      var userId = GetUserId(context.User);
      if (userId is null) return;

      try
      {
        var user = await _userRepository.GetByIdAsync(userId.Value, default);
        if (user is null || !user.DepartmentId.HasValue) return;

        var department = await _departmentRepository.GetByIdAsync(user.DepartmentId.Value, default);
        if (department is null) return;

        if (department.HeadOfDepartmentId == userId.Value)
        {
          context.Succeed(requirement);
        }
      }
      catch
      {
        // If any error occurs during authorization check, deny access
        return;
      }
    }

    private Guid? GetUserId(ClaimsPrincipal user)
    {
      var userIdClaim = user.FindFirst(AppClaimTypes.DbUserId)?.Value;
      return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
  }
}
