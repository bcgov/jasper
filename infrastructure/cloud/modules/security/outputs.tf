
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
    ["ASPNETCORE_URLS", jsondecode(data.aws_secretsmanager_secret_version.aspnet_core_secret_version.secret_string)["urls"]],
    ["Auth__UserId", jsondecode(data.aws_secretsmanager_secret_version.auth_secret_version.secret_string)["userId"]],
    ["Auth__UserPassword", jsondecode(data.aws_secretsmanager_secret_version.auth_secret_version.secret_string)["userPassword"]],
    ["Auth__AllowSiteMinderUserType", jsondecode(data.aws_secretsmanager_secret_version.auth_secret_version.secret_string)["allowSiteMinderUserType"]],
    ["DatabaseConnectionString", jsondecode(data.aws_secretsmanager_secret_version.database_secret_version.secret_string)["dbConnectionString"]],
    ["DataProtectionKeyEncryptionKey", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["dataProtectionKeyEncryptionKey"]],
    ["FileServicesClient__Username", jsondecode(data.aws_secretsmanager_secret_version.file_services_client_secret_version.secret_string)["username"]],
    ["FileServicesClient__Password", jsondecode(data.aws_secretsmanager_secret_version.file_services_client_secret_version.secret_string)["password"]],
    ["FileServicesClient__Url", jsondecode(data.aws_secretsmanager_secret_version.file_services_client_secret_version.secret_string)["baseUrl"]],
    ["Keycloak__Audience", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["audience"]],
    ["Keycloak__Authority", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["authority"]],
    ["Keycloak__Client", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["client"]],
    ["Keycloak__PresReqConfId", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["presReqConfId"]],
    ["Keycloak__Secret", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["secret"]],
    ["Keycloak__VcIdpHint", jsondecode(data.aws_secretsmanager_secret_version.keycloak_secret_version.secret_string)["vcIdpHint"]],
    ["LocationServicesClient__Username", jsondecode(data.aws_secretsmanager_secret_version.location_services_client_secret_version.secret_string)["username"]],
    ["LocationServicesClient__Password", jsondecode(data.aws_secretsmanager_secret_version.location_services_client_secret_version.secret_string)["password"]],
    ["LocationServicesClient__Url", jsondecode(data.aws_secretsmanager_secret_version.location_services_client_secret_version.secret_string)["baseUrl"]],
    ["LookupServicesClient__Username", jsondecode(data.aws_secretsmanager_secret_version.lookup_services_client_secret_version.secret_string)["username"]],
    ["LookupServicesClient__Password", jsondecode(data.aws_secretsmanager_secret_version.lookup_services_client_secret_version.secret_string)["password"]],
    ["LookupServicesClient__Url", jsondecode(data.aws_secretsmanager_secret_version.lookup_services_client_secret_version.secret_string)["baseUrl"]],
    ["Request__ApplicationCd", jsondecode(data.aws_secretsmanager_secret_version.request_secret_version.secret_string)["applicationCd"]],
    ["Request__AgencyIdentifierId", jsondecode(data.aws_secretsmanager_secret_version.request_secret_version.secret_string)["agencyIdentifierId"]],
    ["Request__GetUserLoginDefaultAgencyId", jsondecode(data.aws_secretsmanager_secret_version.request_secret_version.secret_string)["getUserLoginDefaultAgencyId"]],
    ["Request__PartId", jsondecode(data.aws_secretsmanager_secret_version.request_secret_version.secret_string)["partId"]],
    ["SiteMinderLogoutUrl", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["siteMinderLogoutUrl"]],
    ["SplunkCollectorId", jsondecode(data.aws_secretsmanager_secret_version.splunk_secret_version.secret_string)["collectorId"]],
    ["SplunkCollectorUrl", jsondecode(data.aws_secretsmanager_secret_version.splunk_secret_version.secret_string)["collectorUrl"]],
    ["SplunkToken", jsondecode(data.aws_secretsmanager_secret_version.splunk_secret_version.secret_string)["token"]],
    ["UserServicesClient__Username", jsondecode(data.aws_secretsmanager_secret_version.user_services_client_secret_version.secret_string)["username"]],
    ["UserServicesClient__Password", jsondecode(data.aws_secretsmanager_secret_version.user_services_client_secret_version.secret_string)["password"]],
    ["UserServicesClient__Url", jsondecode(data.aws_secretsmanager_secret_version.user_services_client_secret_version.secret_string)["baseUrl"]],
  ]
  sensitive = true
}

output "web_secrets" {
  value = [
    ["API_URL", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["apiUrl"]],
    ["USE_SELF_SIGNED_SSL", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["useSelfSignedSsl"]],
    ["IpFilterRules", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["ipFilterRules"]],
    ["RealIpFrom", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["realIpFrom"]],
    ["WEB_BASE_HREF", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["webBaseHref"]],
    ["IncludeSiteminderHeaders", jsondecode(data.aws_secretsmanager_secret_version.misc_secret_version.secret_string)["includeSiteMinderHeaders"]]
  ]
  sensitive = true
}

output "db_secrets" {
  value = [
    ["POSTGRESQL_USER", jsondecode(data.aws_secretsmanager_secret_version.database_secret_version.secret_string)["user"]],
    ["POSTGRESQL_PASSWORD", jsondecode(data.aws_secretsmanager_secret_version.database_secret_version.secret_string)["password"]],
    ["POSTGRESQL_DATABASE", jsondecode(data.aws_secretsmanager_secret_version.database_secret_version.secret_string)["database"]],
    ["POSTGRESQL_ADMIN_PASSWORD", jsondecode(data.aws_secretsmanager_secret_version.database_secret_version.secret_string)["adminPassword"]]
  ]
  sensitive = true
}
