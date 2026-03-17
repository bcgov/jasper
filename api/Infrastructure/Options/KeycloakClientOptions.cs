using System.ComponentModel.DataAnnotations;

namespace Scv.Api.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for service account token requests.
    /// </summary>
    public class KeycloakClientOptions
    {
        /// <summary>
        /// Keycloak authority URL (e.g., https://keycloak.example.com/realms/your-realm)
        /// </summary>
        [Required]
        public string Authority { get; set; } = default!;

        /// <summary>
        /// Expected audience in the JWT token.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Client ID for the service account.
        /// </summary>
        [Required]
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Service account secret used to retrieve tokens. Value is expected from environment variables.
        /// </summary>
        [Required]
        public string ServiceAccountSecret { get; set; } = default!;

        /// <summary>
        /// Refresh skew in seconds for service account tokens.
        /// </summary>
        [Range(0, 3600)]
        public int RefreshSkewSeconds { get; set; } = 60;

        /// <summary>
        /// Optional token scope for the client credentials request.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Additional clock skew in seconds used when determining token freshness.
        /// </summary>
        [Range(0, 3600)]
        public int ClockSkewSeconds { get; set; } = 0;
    }

    /// <summary>
    /// Keycloak client options for Transitory Documents integrations.
    /// </summary>
    public sealed class TdKeycloakClientOptions : KeycloakClientOptions
    {
    }

    /// <summary>
    /// Keycloak client options for CSO integrations.
    /// </summary>
    public sealed class CsoKeycloakClientOptions : KeycloakClientOptions
    {
    }
}
