output "secrets_arn_list" {
  value = [
    aws_secretsmanager_secret.api_authorizer_secret.arn,
    aws_secretsmanager_secret.aspnet_core_secret.arn,
    aws_secretsmanager_secret.auth_secret.arn,
    aws_secretsmanager_secret.azure_secret.arn,
    aws_secretsmanager_secret.dars_secret.arn,
    aws_secretsmanager_secret.database_secret.arn,
    aws_secretsmanager_secret.file_services_client_secret.arn,
    aws_secretsmanager_secret.jobs_secret.arn,
    aws_secretsmanager_secret.keycloak_secret.arn,
    aws_secretsmanager_secret.location_services_client_secret.arn,
    aws_secretsmanager_secret.lookup_services_client_secret.arn,
    aws_secretsmanager_secret.misc_secret.arn,
    aws_secretsmanager_secret.mtls_cert_secret.arn,
    aws_secretsmanager_secret.nutrient_secret.arn,
    aws_secretsmanager_secret.pcss_secret.arn,
    aws_secretsmanager_secret.request_secret.arn,
    aws_secretsmanager_secret.reserved_judgements_secret.arn,
    aws_secretsmanager_secret.splunk_secret.arn,
    aws_secretsmanager_secret.user_services_client_secret.arn,
    aws_secretsmanager_secret.keycloak_td_secret.arn,
    aws_secretsmanager_secret.smb_secret.arn
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
    ["AZURE__CLIENT_ID", "${aws_secretsmanager_secret.azure_secret.arn}:clientId::"],
    ["AZURE__CLIENT_SECRET", "${aws_secretsmanager_secret.azure_secret.arn}:clientSecret::"],
    ["AZURE__SERVICE_ACCOUNT", "${aws_secretsmanager_secret.azure_secret.arn}:serviceAccount::"],
    ["AZURE__TENANT_ID", "${aws_secretsmanager_secret.azure_secret.arn}:tenantId::"],
    ["DARS__Username", "${aws_secretsmanager_secret.dars_secret.arn}:username::"],
    ["DARS__Password", "${aws_secretsmanager_secret.dars_secret.arn}:password::"],
    ["DARS__Url", "${aws_secretsmanager_secret.dars_secret.arn}:baseUrl::"],
    ["DARS__LogsheetUrl", "${aws_secretsmanager_secret.dars_secret.arn}:logsheetUrl::"],
    ["DatabaseConnectionString", "${aws_secretsmanager_secret.database_secret.arn}:dbConnectionString::"],
    ["DataProtectionKeyEncryptionKey", "${aws_secretsmanager_secret.misc_secret.arn}:dataProtectionKeyEncryptionKey::"],
    ["DEFAULT_QUICK_LINKS", "${aws_secretsmanager_secret.misc_secret.arn}:defaultQuickLinks::"],
    ["FileServicesClient__Username", "${aws_secretsmanager_secret.file_services_client_secret.arn}:username::"],
    ["FileServicesClient__Password", "${aws_secretsmanager_secret.file_services_client_secret.arn}:password::"],
    ["FileServicesClient__Url", "${aws_secretsmanager_secret.file_services_client_secret.arn}:baseUrl::"],
    ["JOBS__SYNC_ASSIGNED_CASES_SCHEDULE", "${aws_secretsmanager_secret.jobs_secret.arn}:syncAssignedCasesSchedule::"],
    ["JOBS__SYNC_DOCUMENT_CATEGORIES_SCHEDULE", "${aws_secretsmanager_secret.jobs_secret.arn}:syncDocumentCategoriesSchedule::"],
    ["JOBS__RETRY_COUNT", "${aws_secretsmanager_secret.jobs_secret.arn}:retryCount::"],
    ["Keycloak__Audience", "${aws_secretsmanager_secret.keycloak_secret.arn}:audience::"],
    ["Keycloak__Authority", "${aws_secretsmanager_secret.keycloak_secret.arn}:authority::"],
    ["Keycloak__Client", "${aws_secretsmanager_secret.keycloak_secret.arn}:client::"],
    ["Keycloak__PresReqConfId", "${aws_secretsmanager_secret.keycloak_secret.arn}:presReqConfId::"],
    ["Keycloak__Secret", "${aws_secretsmanager_secret.keycloak_secret.arn}:secret::"],
    ["Keycloak__VcIdpHint", "${aws_secretsmanager_secret.keycloak_secret.arn}:vcIdpHint::"],
    ["KEY_DOCS_BINDER_REFRESH_HOURS", "${aws_secretsmanager_secret.misc_secret.arn}:keyDocsBinderRefreshHours::"],
    ["LocationServicesClient__Username", "${aws_secretsmanager_secret.location_services_client_secret.arn}:username::"],
    ["LocationServicesClient__Password", "${aws_secretsmanager_secret.location_services_client_secret.arn}:password::"],
    ["LocationServicesClient__Url", "${aws_secretsmanager_secret.location_services_client_secret.arn}:baseUrl::"],
    ["LookupServicesClient__Username", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:username::"],
    ["LookupServicesClient__Password", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:password::"],
    ["LookupServicesClient__Url", "${aws_secretsmanager_secret.lookup_services_client_secret.arn}:baseUrl::"],
    ["MONGODB_CONNECTION_STRING", "${aws_secretsmanager_secret.database_secret.arn}:mongoDbConnectionString::"],
    ["MONGODB_NAME", "${aws_secretsmanager_secret.database_secret.arn}:mongoDbName::"],
    ["NUTRIENT_BE_LICENSE_KEY", "${aws_secretsmanager_secret.nutrient_secret.arn}:nutrientBeLicenseKey::"],
    ["NUTRIENT_FE_LICENSE_KEY", "${aws_secretsmanager_secret.nutrient_secret.arn}:nutrientFeLicenseKey::"],
    ["PCSS__Username", "${aws_secretsmanager_secret.pcss_secret.arn}:username::"],
    ["PCSS__Password", "${aws_secretsmanager_secret.pcss_secret.arn}:password::"],
    ["PCSS__Url", "${aws_secretsmanager_secret.pcss_secret.arn}:baseUrl::"],
    ["PCSS__JudgeId", "${aws_secretsmanager_secret.pcss_secret.arn}:judgeId::"],
    ["PCSS__JudgeHomeLocationId", "${aws_secretsmanager_secret.pcss_secret.arn}:judgeHomeLocationId::"],
    ["Request__ApplicationCd", "${aws_secretsmanager_secret.request_secret.arn}:applicationCd::"],
    ["Request__AgencyIdentifierId", "${aws_secretsmanager_secret.request_secret.arn}:agencyIdentifierId::"],
    ["Request__GetUserLoginDefaultAgencyId", "${aws_secretsmanager_secret.request_secret.arn}:getUserLoginDefaultAgencyId::"],
    ["Request__PartId", "${aws_secretsmanager_secret.request_secret.arn}:partId::"],
    ["RESERVED_JUDGEMENTS__ATTACHMENT_NAME", "${aws_secretsmanager_secret.reserved_judgements_secret.arn}:attachmentName::"],
    ["RESERVED_JUDGEMENTS__SENDER", "${aws_secretsmanager_secret.reserved_judgements_secret.arn}:sender::"],
    ["RESERVED_JUDGEMENTS__SUBJECT", "${aws_secretsmanager_secret.reserved_judgements_secret.arn}:subject::"],
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
    ["IncludeSiteminderHeaders", "${aws_secretsmanager_secret.misc_secret.arn}:includeSiteMinderHeaders::"],
    ["IpFilterRules", "${aws_secretsmanager_secret.misc_secret.arn}:ipFilterRules::"],
    ["RealIpFrom", "${aws_secretsmanager_secret.misc_secret.arn}:realIpFrom::"],
    ["USE_SELF_SIGNED_SSL", "${aws_secretsmanager_secret.misc_secret.arn}:useSelfSignedSsl::"],
    ["WEB_BASE_HREF", "${aws_secretsmanager_secret.misc_secret.arn}:webBaseHref::"]
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

output "allowed_ip_ranges" {
  value     = jsondecode(data.aws_secretsmanager_secret_version.current_misc_secret_value.secret_string).allowedIpRanges
  sensitive = true
}

output "default_users" {
  value = jsonencode(jsondecode(data.aws_secretsmanager_secret_version.current_db_secret_value.secret_string)["defaultUsers"])
}

output "lambda_secrets" {
  value = {
    mtls                 = aws_secretsmanager_secret.mtls_cert_secret.name
    authorizer           = aws_secretsmanager_secret.api_authorizer_secret.name
    authorizer_arn       = aws_secretsmanager_secret.api_authorizer_secret.arn
    file_services_client = aws_secretsmanager_secret.file_services_client_secret.name
    pcss                 = aws_secretsmanager_secret.pcss_secret.name
    dars                 = aws_secretsmanager_secret.dars_secret.name
  }
}
