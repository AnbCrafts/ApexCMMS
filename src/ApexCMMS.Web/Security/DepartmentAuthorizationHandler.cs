using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ApexCMMS.Web.Security;

public class DepartmentRequirement : IAuthorizationRequirement
{
}

public class DepartmentAuthorizationHandler : AuthorizationHandler<DepartmentRequirement, Guid?>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement,
        Guid? resourceDepartmentId)
    {
        // 1. SuperAdmins can access all departments
        if (context.User.IsInRole("SuperAdmin") || context.User.IsInRole("SafetyAuditor"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Resource without department is globally accessible
        if (!resourceDepartmentId.HasValue)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 3. Check if user's DepartmentId matches resource DepartmentId
        var userDeptClaim = context.User.FindFirst("DepartmentId")?.Value;
        if (Guid.TryParse(userDeptClaim, out var userDeptId) && userDeptId == resourceDepartmentId.Value)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
