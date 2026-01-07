namespace Scv.TdApi.Infrastructure.Authorization
{
    /// <summary>
    /// Defines the role names for the Transitory Documents API.
    /// These are client roles from Keycloak.
    /// </summary>
    public static class TdRoles
    {
        /// <summary>
        /// Allows query/search operations (GET /api/documents/search)
        /// </summary>
        public const string Query = "td:query";

        /// <summary>
        /// Allows read/download operations (GET /api/documents/content)
        /// Inherently includes Query permissions.
        /// </summary>
        public const string Read = "td:read";
    }

    /// <summary>
    /// Policy names for authorization
    /// </summary>
    public static class TdPolicies
    {
        public const string RequireQueryRole = "RequireQueryRole";
        public const string RequireReadRole = "RequireReadRole";
    }
}