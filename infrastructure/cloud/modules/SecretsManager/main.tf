resource "aws_secretsmanager_secret" "file_services_client_secret" {
  name       = "external/${var.app_name}-file-services-client-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "file_services_client_secret_value" {
  secret_id = aws_secretsmanager_secret.file_services_client_secret.id
  secret_string = jsonencode({
    username = "",
    password = "",
    baseUrl  = ""
  })
}

resource "aws_secretsmanager_secret" "location_services_client_secret" {
  name       = "external/${var.app_name}-location-services-client-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "location_services_client_secret_value" {
  secret_id = aws_secretsmanager_secret.location_services_client_secret.id
  secret_string = jsonencode({
    username = "",
    password = "",
    baseUrl  = ""
  })
}

resource "aws_secretsmanager_secret" "lookup_services_client_secret" {
  name       = "external/${var.app_name}-lookup-services-client-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "lookup_services_client_secret_value" {
  secret_id = aws_secretsmanager_secret.lookup_services_client_secret.id
  secret_string = jsonencode({
    username = "",
    password = "",
    baseUrl  = ""
  })
}

resource "aws_secretsmanager_secret" "user_services_client_secret" {
  name       = "external/${var.app_name}-user-services-client-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "user_services_client_secret_value" {
  secret_id = aws_secretsmanager_secret.user_services_client_secret.id
  secret_string = jsonencode({
    username = "",
    password = "",
    baseUrl  = ""
  })
}

resource "aws_secretsmanager_secret" "keycloak_secret" {
  name       = "external/${var.app_name}-keycloak-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "keycloak_secret_value" {
  secret_id = aws_secretsmanager_secret.keycloak_secret.id
  secret_string = jsonencode({
    client        = "",
    authority     = "",
    secret        = "",
    audience      = "",
    presReqConfId = "",
    vcIdpHint     = ""
  })
}

resource "aws_secretsmanager_secret" "request_secret" {
  name       = "external/${var.app_name}-request-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "request_secret_value" {
  secret_id = aws_secretsmanager_secret.request_secret.id
  secret_string = jsonencode({
    applicationCd               = "",
    agencyIdentifierId          = "",
    partId                      = "",
    getUserLoginDefaultAgencyId = ""
  })
}

resource "aws_secretsmanager_secret" "splunk_secret" {
  name       = "external/${var.app_name}-splunk-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "splunk_secret_value" {
  secret_id = aws_secretsmanager_secret.splunk_secret.id
  secret_string = jsonencode({
    collectorId  = ""
    collectorUrl = "",
    token        = "",
  })
}

resource "aws_secretsmanager_secret" "database_secret" {
  name       = "external/${var.app_name}-database-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "database_secret_value" {
  secret_id = aws_secretsmanager_secret.database_secret.id
  secret_string = jsonencode({
    dbConnectionString = "",
    user               = "",
    password           = "",
    database           = "",
    adminPassword      = "",
  })
}

data "aws_secretsmanager_secret_version" "current_db_secret_value" {
  secret_id = aws_secretsmanager_secret.database_secret.id
}

resource "aws_secretsmanager_secret" "aspnet_core_secret" {
  name       = "external/${var.app_name}-aspnet-core-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "aspnet_core_secret_value" {
  secret_id = aws_secretsmanager_secret.aspnet_core_secret.id
  secret_string = jsonencode({
    urls        = "",
    environment = "",
  })
}

resource "aws_secretsmanager_secret" "misc_secret" {
  name       = "external/${var.app_name}-misc-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "misc_secret_value" {
  secret_id = aws_secretsmanager_secret.misc_secret.id
  secret_string = jsonencode({
    dataProtectionKeyEncryptionKey = "",
    webBaseHref                    = "",
    useSelfSignedSsl               = "",
    ipFilterRules                  = "",
    realIpFrom                     = "",
    apiUrl                         = ""
    siteMinderLogoutUrl            = "",
    includeSiteMinderHeaders       = "",
    mtlsCert                       = ""
  })
}

resource "aws_secretsmanager_secret" "auth_secret" {
  name       = "external/${var.app_name}-auth-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "auth_secret_value" {
  secret_id = aws_secretsmanager_secret.auth_secret.id
  secret_string = jsonencode({
    userId                  = "",
    userPassword            = "",
    allowSiteMinderUserType = "",
    apigwApiKey             = ""
  })
}

resource "aws_secretsmanager_secret" "mtls_cert_secret" {
  name       = "external/${var.app_name}-mtls-cert-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "mtls_cert_secret_value" {
  secret_id = aws_secretsmanager_secret.mtls_cert_secret.id
  secret_string = jsonencode({
    cert = "",
    key  = "",
    ca   = ""
  })
}

resource "aws_secretsmanager_secret" "api_authorizer_secret" {
  name       = "${var.app_name}-api-authorizer-secret-${var.environment}"
  kms_key_id = var.kms_key_arn

}

resource "random_uuid" "initial_api_auth_value" {}

resource "aws_secretsmanager_secret_rotation" "api_authorizer_secret_rotation" {
  secret_id           = aws_secretsmanager_secret.api_authorizer_secret.id
  rotation_lambda_arn = var.rotate_key_lambda_arn

  rotation_rules {
    schedule_expression = "cron(0 8 * * ? *)" # Daily @ 8AM UTC (12AM PST)
  }
}

resource "aws_secretsmanager_secret_version" "api_authorizer_secret_value" {
  secret_id = aws_secretsmanager_secret.api_authorizer_secret.id
  secret_string = jsonencode({
    "verifyKey" = random_uuid.initial_api_auth_value.result
  })
}
