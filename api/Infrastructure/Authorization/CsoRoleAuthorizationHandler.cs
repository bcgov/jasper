using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.Authorization
{
    /// <summary>
    /// Authorization handler for CSO API roles.
    /// Validates client roles from Keycloak JWT tokens.
    /// </summary>
    public class CSoRoleAuthorizationHandler : AuthorizationHandler<CsoRoleRequirement>
    {
        private readonly ILogger<CSoRoleAuthorizationHandler> _logger;
        private readonly string _writeRoleName;
        private readonly string _audience;

        public CSoRoleAuthorizationHandler(
            ILogger<CSoRoleAuthorizationHandler> logger,
            IOptions<KeycloakOptions> keycloakOptions)
        {
            _logger = logger;
            var options = keycloakOptions?.Value ?? throw new ArgumentNullException(nameof(keycloakOptions));

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                throw new ArgumentException("CsoKeycloak:Audience must be configured.", nameof(keycloakOptions));
            }

            if (string.IsNullOrWhiteSpace(options.WriteRole))
            {
                throw new ArgumentException("CsoKeycloak:WriteRole must be configured.", nameof(keycloakOptions));
            }

            _audience = options.Audience;
            _writeRoleName = options.WriteRole;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CsoRoleRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true || !context.User.IsCsoServiceAccountUser())
            {
                _logger.LogWarning("User is not authenticated");
                context.Fail();
                return Task.CompletedTask;
            }

            var clientRoles = context.User.ClientRoles(_audience);

            if (clientRoles == null || !clientRoles.Any())
            {
                _logger.LogWarning(
                    "No client roles found for audience: {ClientId}",
                    _audience);
                context.Fail();
                return Task.CompletedTask;
            }

            var hasRequiredRole =
                string.Equals(requirement.RequiredRole, _writeRoleName, StringComparison.OrdinalIgnoreCase) &&
                clientRoles.Any(role => role.Equals(_writeRoleName, StringComparison.OrdinalIgnoreCase));

            if (hasRequiredRole)
            {
                var username = context.User.PreferredUsername();

                _logger.LogInformation(
                    "Authorization succeeded for user: {Username}, required role: {RequiredRole}",
                    username,
                    requirement.RequiredRole);

                context.Succeed(requirement);
            }
            else
            {
                var username = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("preferred_username")?.Value
                    ?? "unknown";

                _logger.LogWarning(
                    "Authorization failed for user: {Username}, required role: {RequiredRole}, user roles: {UserRoles}",
                    username,
                    requirement.RequiredRole,
                    string.Join(", ", clientRoles));

                context.Fail();
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Authorization requirement for role-based access
    /// </summary>
    public class CsoRoleRequirement(string requiredRole) : IAuthorizationRequirement
    {
        public string RequiredRole { get; } = requiredRole;
    }
}