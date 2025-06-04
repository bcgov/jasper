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
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS),
            new(CustomClaimTypes.Permission, Permission.VIEW_CASE_DETAILS)
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
    public void HasPermissions_ShouldReturnTrue_WhenClaimsHaveExactPermissions()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS)
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
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS),
            new(CustomClaimTypes.Permission, Permission.VIEW_CASE_DETAILS)
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
    public void HasPermissions_ShouldReturnFalse_WhenClaimsAreNotExactPermissions()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasPermissions([
            Permission.LOCK_UNLOCK_USERS,
            Permission.VIEW_SENTENCE_ORDER_DETAILS
        ], false);

        Assert.False(result);
    }

    [Fact]
    public void HasRoles_ShouldReturnTrue_WhenClaimsHasAtleastOneMatchedRole()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Role, Role.ACJ_CHIEF_JUDGE),
            new(CustomClaimTypes.Role, Role.ADMIN),
            new(CustomClaimTypes.Role, Role.JUDGE)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasRoles([Role.ADMIN], true);

        Assert.True(result);
    }

    [Fact]
    public void HasRoles_ShouldReturnTrue_WhenClaimsHaveExactRoles()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Role, Role.ACJ_CHIEF_JUDGE),
            new(CustomClaimTypes.Role, Role.ADMIN)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasRoles([
            Role.ACJ_CHIEF_JUDGE,
            Role.ADMIN
        ], false);

        Assert.True(result);
    }

    [Fact]
    public void HasRoles_ShouldReturnFalse_WhenClaimsHasNoMatchedRole()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Role, Role.ACJ_CHIEF_JUDGE),
            new(CustomClaimTypes.Role, Role.ADMIN),
            new(CustomClaimTypes.Role, Role.OCJ_SERVICE_DESK)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasRoles([
            Role.JUDGE,
            Role.RAJ
       ], true);

        Assert.False(result);
    }

    [Fact]
    public void HasRoles_ShouldReturnFalse_WhenClaimsAreNotExactRoles()
    {
        var claims = new List<Claim>{
            new(CustomClaimTypes.Role, Role.ACJ_CHIEF_JUDGE),
            new(CustomClaimTypes.Role, Role.ADMIN)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = user.HasRoles([
            Role.TRAINER,
            Role.OCJ_SERVICE_DESK
        ], false);

        Assert.False(result);
    }
}
