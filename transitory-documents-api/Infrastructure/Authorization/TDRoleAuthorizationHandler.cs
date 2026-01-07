using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Scv.TdApi.Infrastructure.Options;
using System.Security.Claims;

namespace Scv.TdApi.Infrastructure.Authorization
{
    /// <summary>
    /// Authorization handler for Transitory Documents API roles.
    /// Validates client roles from Keycloak JWT tokens.
    /// </summary>
    public class TdRoleAuthorizationHandler : AuthorizationHandler<TdRoleRequirement>
    {
        private readonly KeycloakOptions _keycloakOptions;
        private readonly ILogger<TdRoleAuthorizationHandler> _logger;

        public TdRoleAuthorizationHandler(
            IOptions<KeycloakOptions> keycloakOptions,
            ILogger<TdRoleAuthorizationHandler> logger)
        {
            _keycloakOptions = keycloakOptions.Value;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TdRoleRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User is not authenticated");
                context.Fail();
                return Task.CompletedTask;
            }

            // Extract client roles from resource_access claim
            var resourceAccessClaim = context.User.FindFirst("resource_access")?.Value;

            if (string.IsNullOrWhiteSpace(resourceAccessClaim))
            {
                _logger.LogWarning("No resource_access claim found in token");
                context.Fail();
                return Task.CompletedTask;
            }

            var clientRoles = ExtractClientRoles(resourceAccessClaim, _keycloakOptions.ClientId);

            if (clientRoles == null || !clientRoles.Any())
            {
                _logger.LogWarning(
                    "No client roles found for client: {ClientId}",
                    _keycloakOptions.ClientId);
                context.Fail();
                return Task.CompletedTask;
            }

            var hasRequiredRole = requirement.RequiredRole switch
            {
                TdRoles.Query => HasQueryRole(clientRoles),
                TdRoles.Read => HasReadRole(clientRoles),
                _ => false
            };

            if (hasRequiredRole)
            {
                var username = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("preferred_username")?.Value
                    ?? "unknown";

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

        private bool HasQueryRole(string[] clientRoles)
        {
            return clientRoles.Contains(_keycloakOptions.QueryRole, System.StringComparer.OrdinalIgnoreCase) ||
                   clientRoles.Contains(_keycloakOptions.ReadRole, System.StringComparer.OrdinalIgnoreCase); // Read includes Query
        }

        private bool HasReadRole(string[] clientRoles)
        {
            return clientRoles.Contains(_keycloakOptions.ReadRole, System.StringComparer.OrdinalIgnoreCase);
        }

        private static string[]? ExtractClientRoles(string resourceAccessJson, string clientId)
        {
            try
            {
                var resourceAccess = System.Text.Json.JsonDocument.Parse(resourceAccessJson);

                if (resourceAccess.RootElement.TryGetProperty(clientId, out var clientElement))
                {
                    if (clientElement.TryGetProperty("roles", out var rolesElement))
                    {
                        return rolesElement.EnumerateArray()
                            .Select(r => r.GetString())
                            .Where(r => !string.IsNullOrWhiteSpace(r))
                            .ToArray()!;
                    }
                }

                return null;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Authorization requirement for role-based access
    /// </summary>
    public class TdRoleRequirement(string requiredRole) : IAuthorizationRequirement
    {
        public string RequiredRole { get; } = requiredRole;
    }
}