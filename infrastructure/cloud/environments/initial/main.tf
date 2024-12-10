# This "initial" stack is deployed first to avoid circular dependency to other resources

# Create Openshift User
module "dynamo_db" {
  source              = "../../modules/DynamoDb"
  iam_user_table_name = var.iam_user_table_name
  openshift_iam_user  = var.openshift_iam_user
}

# Custom KMS Key
module "kms" {
  source             = "../../modules/KMS"
  environment        = var.environment
  app_name           = var.app_name
  region             = var.region
  kms_key_name       = var.kms_key_name
  openshift_iam_user = var.openshift_iam_user
  depends_on         = [module.dynamo_db]
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
