using Microsoft.AspNetCore.Authorization;

namespace Scv.Core.Infrastructure.Authorization;

public class PermissionRequirement(bool applyOrCondition = false, params string[] permissions) : IAuthorizationRequirement
{
    public bool ApplyOrCondition { get; } = applyOrCondition;
    public List<string> RequiredPermissions { get; } = [.. permissions];
}
