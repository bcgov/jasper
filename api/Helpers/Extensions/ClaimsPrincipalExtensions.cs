using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Scv.Api.Helpers.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string ApplicationCode(this ClaimsPrincipal claimsPrincipal)
        {
            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            return identity.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.ApplicationCode)?.Value;
        }

        public static string AgencyCode(this ClaimsPrincipal claimsPrincipal)
        {
            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            return identity.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.JcAgencyCode)?.Value;
        }

        public static string ParticipantId(this ClaimsPrincipal claimsPrincipal)
        {
            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            return identity.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.JcParticipantId)?.Value;
        }

        public static string UserId(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        public static string PreferredUsername(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(CustomClaimTypes.PreferredUsername);

        public static List<string> Groups(this ClaimsPrincipal claimsPrincipal)
        {
            var identity = (ClaimsIdentity)claimsPrincipal.Identity;
            return identity.Claims.Where(c => c.Type == CustomClaimTypes.Groups).Select(s => s.Value).ToList();
        }

        public static bool IsServiceAccountUser(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.PreferredUsername) &&
               claimsPrincipal.FindFirstValue(CustomClaimTypes.PreferredUsername).Equals("service-account-scv");

        public static bool IsIdirUser(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.PreferredUsername) &&
           claimsPrincipal.FindFirstValue(CustomClaimTypes.PreferredUsername).EndsWith("@idir");

        public static bool IsVcUser(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.PreferredUsername) &&
               claimsPrincipal.FindFirstValue(CustomClaimTypes.PreferredUsername).EndsWith("@vc");

        public static bool IsSupremeUser(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.IsSupremeUser) &&
           claimsPrincipal.FindFirstValue(CustomClaimTypes.IsSupremeUser).Equals("true", StringComparison.OrdinalIgnoreCase);

        public static string Role(this ClaimsPrincipal claimsPrincipal) =>
           claimsPrincipal.FindFirstValue(CustomClaimTypes.Role);

        public static string SubRole(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(CustomClaimTypes.SubRole);

        public static bool IsStaff(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.Role) &&
               claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.SubRole) &&
               claimsPrincipal.FindFirstValue(CustomClaimTypes.Role).Equals("EME") &&
               claimsPrincipal.FindFirstValue(CustomClaimTypes.SubRole).Equals("SCV");

        public static string Email(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(ClaimTypes.Email);

        public static bool HasPermissions(
            this ClaimsPrincipal claimsPrincipal,
            List<string> requiredPermissions,
            bool applyOrCondition = false)
        {
            return applyOrCondition
                // At least one permission is present
                ? requiredPermissions
                    .Any(code => claimsPrincipal.HasClaim(CustomClaimTypes.PermissionClaim, code))
                // All permissions must be present
                : requiredPermissions.All(code => claimsPrincipal.HasClaim(CustomClaimTypes.PermissionClaim, code));
        }

        public static List<string> Permissions(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindAll(CustomClaimTypes.PermissionClaim)?.Select(c => c.Value).ToList() ?? [];
    }
}
