using System.ComponentModel.DataAnnotations;

namespace Scv.Api.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for Keycloak authentication and authorization.
    /// </summary>
    public sealed class KeycloakOptions
    {
        /// <summary>
        /// Keycloak authority URL (e.g., https://keycloak.example.com/realms/your-realm)
        /// </summary>
        [Required]
        public string Authority { get; set; } = default!;

        /// <summary>
        /// Expected audience in the JWT token (default: td-dev)
        /// </summary>
        [Required]
        public string Audience { get; set; } = "jasper";

        /// <summary>
        /// Client ID for the service account (default: jasper-td-dev)
        /// </summary>
        [Required]
        public string ClientId { get; set; } = "cso-jasper-dev";

        /// <summary>
        /// Client role name that allows query/search operations
        /// </summary>
        [Required]
        public string WriteRole { get; set; } = "cso-order-write";

        /// <summary>
        /// Validate the token issuer (default: true)
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Require HTTPS metadata (default: true, set to false only for local dev)
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;
    }
}