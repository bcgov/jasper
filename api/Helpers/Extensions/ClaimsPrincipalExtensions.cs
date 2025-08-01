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

        public static string ExternalUserId(this ClaimsPrincipal claimsPrincipal) =>
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

        public static string ExternalRole(this ClaimsPrincipal claimsPrincipal) =>
           claimsPrincipal.FindFirstValue(CustomClaimTypes.ExternalRole);

        public static string SubRole(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(CustomClaimTypes.SubRole);

        public static bool IsStaff(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.ExternalRole) &&
               claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.SubRole) &&
               claimsPrincipal.FindFirstValue(CustomClaimTypes.ExternalRole).Equals("EME") &&
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
                    .Any(code => claimsPrincipal.HasClaim(CustomClaimTypes.Permission, code))
                // All permissions must be present
                : requiredPermissions.All(code => claimsPrincipal.HasClaim(CustomClaimTypes.Permission, code));
        }

        public static bool HasRoles(
            this ClaimsPrincipal claimsPrincipal,
            List<string> requiredRoles,
            bool applyOrCondition = false)
        {
            return applyOrCondition
                // At least one role is present
                ? requiredRoles
                    .Any(name => claimsPrincipal.HasClaim(CustomClaimTypes.Role, name))
                // All roles must be present
                : requiredRoles.All(name => claimsPrincipal.HasClaim(CustomClaimTypes.Role, name));
        }

        public static List<string> Permissions(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindAll(CustomClaimTypes.Permission)?.Select(c => c.Value).ToList() ?? [];

        public static List<string> Roles(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindAll(CustomClaimTypes.Role)?.Select(c => c.Value).ToList() ?? [];

        public static string UserId(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue(CustomClaimTypes.UserId);

        public static int JudgeId(this ClaimsPrincipal claimsPrincipal, int? judgeIdOverride = null)
        {
            if (judgeIdOverride != null && CanViewOthersSchedule(claimsPrincipal))
            {
                return judgeIdOverride.GetValueOrDefault();
            }

            var value = claimsPrincipal.FindFirstValue(CustomClaimTypes.JudgeId);

            return int.TryParse(value, out var userId) ? userId : default;
        }

        public static bool CanViewOthersSchedule(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.Groups && c.Value == "jasper-view-others-schedule");
    }
}
