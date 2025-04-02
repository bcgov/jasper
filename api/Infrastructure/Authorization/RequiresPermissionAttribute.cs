using System;
using Microsoft.AspNetCore.Mvc;

namespace Scv.Api.Infrastructure.Authorization;


[AttributeUsage(AttributeTargets.Method)]
public class RequiresPermissionAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Allows declarative claims based permissions to be applied to controller methods for authorization.
    /// </summary>
    public RequiresPermissionAttribute(bool applyOrCondition = false, params string[] permissions) : base(typeof(RequiresPermissionFilter))
    {
        Arguments = [new PermissionRequirement(applyOrCondition, permissions)];
    }
}
