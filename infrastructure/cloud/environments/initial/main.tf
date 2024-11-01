# This "initial" stack is deployed first to avoid circular dependency to other resources
data "aws_caller_identity" "current" {}

# Custom KMS Key
module "kms" {
  source             = "../../modules/KMS"
  environment        = var.environment
  app_name           = var.app_name
  region             = var.region
  kms_key_name       = var.kms_key_name
  openshift_iam_user = var.openshift_iam_user
  account_id         = data.aws_caller_identity.current.account_id
}

# UI and API ECR repository
module "app_ecr" {
  source      = "../../modules/ECR"
  environment = var.environment
  app_name    = var.app_name
  region      = var.region
  kms_key_id  = module.kms.kms_key_id
  repo_name   = "app"
}

# Lambda functions ECR repository
module "lambda_ecr" {
  source      = "../../modules/ECR"
  environment = var.environment
  app_name    = var.app_name
  region      = var.region
  kms_key_id  = module.kms.kms_key_id
  repo_name   = "lambda"
}
