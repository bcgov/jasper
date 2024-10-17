
output "kms_key_alias" {
  value = aws_kms_alias.kms_alias.name
}

output "ecs_execution_role_arn" {
  value = aws_iam_role.ecs_execution_role.arn
}

output "kms_key_id" {
  value = aws_kms_key.kms_key.key_id
}

output "kms_key_arn" {
  value = aws_kms_key.kms_key.arn
}

output "api_secrets" {
  value = [
    ["ASPNETCORE_URLS", "${aws_secretsmanager_secret.aspnet_core_secret.arn}:json-key/urls}"],
    ["Auth__UserId", "${aws_secretsmanager_secret.auth_secret.arn}:json-key/userId}"],
    ["Auth__UserPassword", "${aws_secretsmanager_secret.auth_secret.arn}:json-key/userPassword}"],
    ["Auth__AllowSiteMinderUserType", "${aws_secretsmanager_secret.auth_secret.arn}:json-key/allowSiteMinderUserType}"],
    ["CORS_DOMAIN", var.lb_dns_name],
    ["DatabaseConnectionString", "${aws_secretsmanager_secret.database_secret.arn}:json-key/dbConnectionString}"],
    ["DataProtectionKeyEncryptionKey", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/dataProtectionKeyEncryptionKey}"],
    ["FileServicesClient__Username", "${aws_secretsmanager_secret.file_services_client_secret.arn}:json-key/username}"],
    ["FileServicesClient__Password", "${aws_secretsmanager_secret.file_services_client_secret.arn}:json-key/password}"],
    ["FileServicesClient__Url", "${aws_secretsmanager_secret.file_services_client_secret.arn}:json-key/baseUrl}"],
    ["Keycloak__Audience", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/audience}"],
    ["Keycloak__Authority", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/authority}"],
    ["Keycloak__Client", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/client}"],
    ["Keycloak__PresReqConfId", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/presReqConfId}"],
    ["Keycloak__Secret", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/secret}"],
    ["Keycloak__VcIdpHint", "${aws_secretsmanager_secret.keycloak_secret.arn}:json-key/vcIdpHint}"],
    ["LocationServicesClient__Username", "${aws_secretsmanager_secret.location_services_client_secret.arn}:json-key/username}"],
    ["LocationServicesClient__Password", "${aws_secretsmanager_secret.location_services_client_secret.arn}:json-key/password}"],
    ["LocationServicesClient__Url", "${aws_secretsmanager_secret.location_services_client_secret.arn}:json-key/baseUrl}"],
    ["LookupServicesClient__Username", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:json-key/username}"],
    ["LookupServicesClient__Password", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:json-key/password}"],
    ["LookupServicesClient__Url", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:json-key/baseUrl}"],
    ["Request__ApplicationCd", "${aws_secretsmanager_secret.request_secret.arn}:json-key/applicationCd}"],
    ["Request__AgencyIdentifierId", "${aws_secretsmanager_secret.request_secret.arn}:json-key/agencyIdentifierId}"],
    ["Request__GetUserLoginDefaultAgencyId", "${aws_secretsmanager_secret.request_secret.arn}:json-key/getUserLoginDefaultAgencyId}"],
    ["Request__PartId", "${aws_secretsmanager_secret.request_secret.arn}:json-key/partId}"],
    ["SiteMinderLogoutUrl", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/siteMinderLogoutUrl}"],
    ["SplunkCollectorId", "${aws_secretsmanager_secret.splunk_secret.arn}:json-key/collectorId}"],
    ["SplunkCollectorUrl", "${aws_secretsmanager_secret.splunk_secret.arn}:json-key/collectorUrl}"],
    ["SplunkToken", "${aws_secretsmanager_secret.splunk_secret.arn}:json-key/token}"],
    ["UserServicesClient__Username", "${aws_secretsmanager_secret.user_services_client_secret.arn}:json-key/username}"],
    ["UserServicesClient__Password", "${aws_secretsmanager_secret.user_services_client_secret.arn}:json-key/password}"],
    ["UserServicesClient__Url", "${aws_secretsmanager_secret.user_services_client_secret.arn}:json-key/baseUrl}"]
  ]
}

output "web_secrets" {
  value = [
    ["API_URL", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/apiUrl}"],
    ["USE_SELF_SIGNED_SSL", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/useSelfSignedSsl}"],
    ["IpFilterRules", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/ipFilterRules}"],
    ["RealIpFrom", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/realIpFrom}"],
    ["WEB_BASE_HREF", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/webBaseHref}"],
    ["IncludeSiteminderHeaders", "${aws_secretsmanager_secret.misc_secret.arn}:json-key/includeSiteMinderHeaders}"]
  ]
}

output "db_secrets" {
  value = [
    ["POSTGRES_USER", "${aws_secretsmanager_secret.database_secret.arn}:json-key/user}"],
    ["POSTGRES_PASSWORD", "${aws_secretsmanager_secret.database_secret.arn}:json-key/password}"],
    ["POSTGRES_DB", "${aws_secretsmanager_secret.database_secret.arn}:json-key/database}"]
  ]
}
