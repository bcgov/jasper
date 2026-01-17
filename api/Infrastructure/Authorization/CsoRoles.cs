namespace Scv.Api.Infrastructure.Authorization
{
    /// <summary>
    /// Defines the role names for the JASPER CSO integration.
    /// These are client roles from Keycloak.
    /// </summary>
    public static class CsoRoles
    {
        /// <summary>
        /// Allows write order operations from CSO. maps to the CsoAuthorization:WriteRoleName configuration option.
        /// </summary>
        public const string Write = "cso:write";
    }

    /// <summary>
    /// Policy names for authorization. These policies determine which roles are required to access certain resources.
    /// </summary>
    public static class CsoPolicies
    {
        public const string RequireWriteRole = nameof(CSoRoleAuthorizationHandler);
        public const string AuthenticationScheme = "CsoKeycloak";
    }
}