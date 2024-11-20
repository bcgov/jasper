output "secrets_arn_list" {
  value = [
    aws_secretsmanager_secret.aspnet_core_secret.arn,
    aws_secretsmanager_secret.auth_secret.arn,
    aws_secretsmanager_secret.database_secret.arn,
    aws_secretsmanager_secret.file_services_client_secret.arn,
    aws_secretsmanager_secret.keycloak_secret.arn,
    aws_secretsmanager_secret.location_services_client_secret.arn,
    aws_secretsmanager_secret.lookup_services_client_secret.arn,
    aws_secretsmanager_secret.misc_secret.arn,
    aws_secretsmanager_secret.mtls_cert_secret.arn,
    aws_secretsmanager_secret.request_secret.arn,
    aws_secretsmanager_secret.splunk_secret.arn,
    aws_secretsmanager_secret.user_services_client_secret.arn,
    aws_secretsmanager_secret.api_authorizer_secret.arn
  ]
}

output "api_secrets" {
  value = [
    ["ASPNETCORE_URLS", "${aws_secretsmanager_secret.aspnet_core_secret.arn}:urls::"],
    ["ASPNETCORE_ENVIRONMENT", "${aws_secretsmanager_secret.aspnet_core_secret.arn}:environment::"],
    ["AWS_API_GATEWAY_API_KEY", "${aws_secretsmanager_secret.auth_secret.arn}:apigwApiKey::"],
    ["Auth__UserId", "${aws_secretsmanager_secret.auth_secret.arn}:userId::"],
    ["Auth__UserPassword", "${aws_secretsmanager_secret.auth_secret.arn}:userPassword::"],
    ["Auth__AllowSiteMinderUserType", "${aws_secretsmanager_secret.auth_secret.arn}:allowSiteMinderUserType::"],
    ["AuthorizerKey", "${aws_secretsmanager_secret.api_authorizer_secret.arn}:verifyKey::"],
    ["DatabaseConnectionString", "${aws_secretsmanager_secret.database_secret.arn}:dbConnectionString::"],
    ["DataProtectionKeyEncryptionKey", "${aws_secretsmanager_secret.misc_secret.arn}:dataProtectionKeyEncryptionKey::"],
    ["FileServicesClient__Username", "${aws_secretsmanager_secret.file_services_client_secret.arn}:username::"],
    ["FileServicesClient__Password", "${aws_secretsmanager_secret.file_services_client_secret.arn}:password::"],
    ["FileServicesClient__Url", "${aws_secretsmanager_secret.file_services_client_secret.arn}:baseUrl::"],
    ["Keycloak__Audience", "${aws_secretsmanager_secret.keycloak_secret.arn}:audience::"],
    ["Keycloak__Authority", "${aws_secretsmanager_secret.keycloak_secret.arn}:authority::"],
    ["Keycloak__Client", "${aws_secretsmanager_secret.keycloak_secret.arn}:client::"],
    ["Keycloak__PresReqConfId", "${aws_secretsmanager_secret.keycloak_secret.arn}:presReqConfId::"],
    ["Keycloak__Secret", "${aws_secretsmanager_secret.keycloak_secret.arn}:secret::"],
    ["Keycloak__VcIdpHint", "${aws_secretsmanager_secret.keycloak_secret.arn}:vcIdpHint::"],
    ["LocationServicesClient__Username", "${aws_secretsmanager_secret.location_services_client_secret.arn}:username::"],
    ["LocationServicesClient__Password", "${aws_secretsmanager_secret.location_services_client_secret.arn}:password::"],
    ["LocationServicesClient__Url", "${aws_secretsmanager_secret.location_services_client_secret.arn}:baseUrl::"],
    ["LookupServicesClient__Username", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:username::"],
    ["LookupServicesClient__Password", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:password::"],
    ["LookupServicesClient__Url", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:baseUrl::"],
    ["Request__ApplicationCd", "${aws_secretsmanager_secret.request_secret.arn}:applicationCd::"],
    ["Request__AgencyIdentifierId", "${aws_secretsmanager_secret.request_secret.arn}:agencyIdentifierId::"],
    ["Request__GetUserLoginDefaultAgencyId", "${aws_secretsmanager_secret.request_secret.arn}:getUserLoginDefaultAgencyId::"],
    ["Request__PartId", "${aws_secretsmanager_secret.request_secret.arn}:partId::"],
    ["SiteMinderLogoutUrl", "${aws_secretsmanager_secret.misc_secret.arn}:siteMinderLogoutUrl::"],
    ["SplunkCollectorId", "${aws_secretsmanager_secret.splunk_secret.arn}:collectorId::"],
    ["SplunkCollectorUrl", "${aws_secretsmanager_secret.splunk_secret.arn}:collectorUrl::"],
    ["SplunkToken", "${aws_secretsmanager_secret.splunk_secret.arn}:token::"],
    ["UserServicesClient__Username", "${aws_secretsmanager_secret.user_services_client_secret.arn}:username::"],
    ["UserServicesClient__Password", "${aws_secretsmanager_secret.user_services_client_secret.arn}:password::"],
    ["UserServicesClient__Url", "${aws_secretsmanager_secret.user_services_client_secret.arn}:baseUrl::"]
  ]
}

output "web_secrets" {
  value = [
    ["API_URL", "${aws_secretsmanager_secret.misc_secret.arn}:apiUrl::"],
    ["USE_SELF_SIGNED_SSL", "${aws_secretsmanager_secret.misc_secret.arn}:useSelfSignedSsl::"],
    ["IpFilterRules", "${aws_secretsmanager_secret.misc_secret.arn}:ipFilterRules::"],
    ["RealIpFrom", "${aws_secretsmanager_secret.misc_secret.arn}:realIpFrom::"],
    ["WEB_BASE_HREF", "${aws_secretsmanager_secret.misc_secret.arn}:webBaseHref::"],
    ["IncludeSiteminderHeaders", "${aws_secretsmanager_secret.misc_secret.arn}:includeSiteMinderHeaders::"]
  ]
}

output "db_username" {
  value     = jsondecode(data.aws_secretsmanager_secret_version.current_db_secret_value.secret_string).user
  sensitive = true
}

output "db_password" {
  value     = jsondecode(data.aws_secretsmanager_secret_version.current_db_secret_value.secret_string).password
  sensitive = true
}

output "mtls_secret_name" {
  value = aws_secretsmanager_secret.mtls_cert_secret.name
}

output "api_authorizer_secret" {
  value = aws_secretsmanager_secret.api_authorizer_secret
}