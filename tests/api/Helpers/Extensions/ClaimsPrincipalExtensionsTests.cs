using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Helpers.Extensions;

public class ClaimsPrincipalExtensionsTests
{

    [Fact]
    public void HasPermissions_ShouldReturnTrue_WhenClaimsHasAtleastOneMatchedPermission()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.PermissionClaim, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.PermissionClaim, Permission.ACCESS_DARS),
            new(CustomClaimTypes.PermissionClaim, Permission.VIEW_CASE_DETAILS)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasPermissions([
            Permission.VIEW_CASE_DETAILS,
            Permission.VIEW_ADJUDICATOR_RESTRICTIONS
       ], true);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissions_ShouldReturnTrue_WhenClaimsHaveExactPermissions()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.PermissionClaim, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.PermissionClaim, Permission.ACCESS_DARS)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasPermissions([
            Permission.LOCK_UNLOCK_USERS,
            Permission.ACCESS_DARS
        ], false);

        Assert.True(result);
    }

    [Fact]
    public void HasPermissions_ShouldReturnFalse_WhenClaimsHasNoMatchedPermission()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.PermissionClaim, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.PermissionClaim, Permission.ACCESS_DARS),
            new(CustomClaimTypes.PermissionClaim, Permission.VIEW_CASE_DETAILS)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasPermissions([
            Permission.VIEW_SINGLE_DOCUMENT,
            Permission.VIEW_ADJUDICATOR_RESTRICTIONS
       ], true);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissions_ShouldReturnFalse_WhenClaimsAreNotExactPermissions()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.PermissionClaim, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.PermissionClaim, Permission.ACCESS_DARS)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasPermissions([
            Permission.LOCK_UNLOCK_USERS,
            Permission.VIEW_SENTENCE_ORDER_DETAILS
        ], false);

        Assert.False(result);
    }
}
