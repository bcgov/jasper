using Scv.Api.Models.AccessControlManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Scv.Api.Helpers.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        private const string JUDICIARY = "judiciary";
        private const string VC = "vc";
        private const string IDIR = "idir";
        private const string JUDGE = "Judge";

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
            if (claimsPrincipal?.Identity is not ClaimsIdentity identity)
                return [];

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

        public static bool IsActive(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.HasClaim(c => c.Type == CustomClaimTypes.IsActive) &&
           claimsPrincipal.FindFirstValue(CustomClaimTypes.IsActive).Equals("true", StringComparison.OrdinalIgnoreCase);

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

        public static string FullName(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue("name");

        public static string FirstName(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue("given_name");

        public static string UserType(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.IsIdirUser())
                return IDIR;
            if (claimsPrincipal.IsVcUser())
                return VC;
                
            return JUDICIARY;
        }

        public static string UserTitle(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.UserType() == JUDICIARY
                ? JUDGE + " " + claimsPrincipal.LastName()
                : claimsPrincipal.FirstName() + " " + claimsPrincipal.LastName();

        public static string LastName(this ClaimsPrincipal claimsPrincipal) =>
            claimsPrincipal.FindFirstValue("family_name");

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
            if (claimsPrincipal == null)
                return default;

            if (judgeIdOverride != null && CanViewOthersSchedule(claimsPrincipal))
            {
                return judgeIdOverride.GetValueOrDefault();
            }

            var value = claimsPrincipal.FindFirstValue(CustomClaimTypes.JudgeId);

            return int.TryParse(value, out var userId) ? userId : default;
        }

        public static int JudgeHomeLocationId(this ClaimsPrincipal claimsPrincipal)
        {
            var value = claimsPrincipal.FindFirstValue(CustomClaimTypes.JudgeHomeLocationId);
            return int.TryParse(value, out var id) ? id : default;
        }

        public static bool CanViewOthersSchedule(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal?.HasClaim(c => c.Type == CustomClaimTypes.Groups && c.Value == "jasper-view-others-schedule") ?? false;

        public static string IdirUserGuid(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.FindFirstValue(CustomClaimTypes.UserGuid);

        public static string ProvjudUserGuid(this ClaimsPrincipal claimsPrincipal)
        {
            var base64Guid = claimsPrincipal.FindFirstValue(CustomClaimTypes.ProvjudUserGuid);
            if (string.IsNullOrWhiteSpace(base64Guid))
            {
                return null;
            }
            byte[] decodedBytes = Convert.FromBase64String(base64Guid);
            string hex = Convert.ToHexStringLower(decodedBytes);
            return hex;
        }

        public static string ExternalJudgeId(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.FindFirstValue(CustomClaimTypes.ExternalJudgeId);

        // Check if any of the user's claims have meaningfully changed compared to the current user data
        public static bool HasChanged(this ClaimsPrincipal claimsPrincipal, UserDto currentUser)
            => claimsPrincipal.IsActive() != currentUser.IsActive ||
            !claimsPrincipal.Roles().Order().SequenceEqual(currentUser.Roles.Order()) ||
            !claimsPrincipal.Permissions().Order().SequenceEqual(currentUser.Permissions.Order());
    }
}
