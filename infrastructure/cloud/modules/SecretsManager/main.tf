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
  lifecycle {
    ignore_changes = [secret_string]
  }

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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "database_secret" {
  name       = "external/${var.app_name}-database-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "database_secret_value" {
  secret_id = aws_secretsmanager_secret.database_secret.id
  secret_string = jsonencode({
    dbConnectionString      = "",
    user                    = "",
    password                = "",
    database                = "",
    adminPassword           = "",
    mongoDbConnectionString = "",
    mongoDbName             = "",
    defaultUsers            = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
    mtlsCert                       = "",
    allowedIpRanges                = "",
    keyDocsBinderRefreshHours      = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "nutrient_secret" {
  name       = "external/${var.app_name}-nutrient-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "nutrient_secret_value" {
  secret_id = aws_secretsmanager_secret.nutrient_secret.id
  secret_string = jsonencode({
    nutrientFeLicenseKey = "",
    nutrientBeLicenseKey = "",
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}


data "aws_secretsmanager_secret_version" "current_misc_secret_value" {
  secret_id = aws_secretsmanager_secret.misc_secret.id
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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
  lifecycle {
    ignore_changes = [secret_string]
  }
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

resource "aws_secretsmanager_secret" "pcss_secret" {
  name       = "external/${var.app_name}-pcss-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "pcss_secret_value" {
  secret_id = aws_secretsmanager_secret.pcss_secret.id
  secret_string = jsonencode({
    username            = "",
    password            = "",
    baseUrl             = "",
    judgeId             = "",
    judgeHomeLocationId = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "dars_secret" {
  name       = "external/${var.app_name}-dars-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "dars_secret_value" {
  secret_id = aws_secretsmanager_secret.dars_secret.id
  secret_string = jsonencode({
    username = "",
    password = "",
    baseUrl  = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "azure_secret" {
  name       = "external/${var.app_name}-azure-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "azure_secret_value" {
  secret_id = aws_secretsmanager_secret.azure_secret.id
  secret_string = jsonencode({
    clientId       = "",
    clientSecret   = ""
    tenantId       = "",
    serviceAccount = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "jobs_secret" {
  name       = "external/${var.app_name}-jobs-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "jobs_secret_value" {
  secret_id = aws_secretsmanager_secret.jobs_secret.id
  secret_string = jsonencode({
    syncAssignedCasesSchedule      = "",
    syncDocumentCategoriesSchedule = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}

resource "aws_secretsmanager_secret" "reserved_judgements_secret" {
  name       = "external/${var.app_name}-reserved-judgements-secret-${var.environment}"
  kms_key_id = var.kms_key_arn
}

resource "aws_secretsmanager_secret_version" "reserved_judgements_secret_value" {
  secret_id = aws_secretsmanager_secret.reserved_judgements_secret.id
  secret_string = jsonencode({
    attachmentName = "",
    sender         = "",
    subject        = ""
  })
  lifecycle {
    ignore_changes = [secret_string]
  }
}
