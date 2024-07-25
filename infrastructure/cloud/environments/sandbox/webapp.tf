

module "security" {
  source       = "../../modules/security"
  environment  = var.environment
  app_name     = var.app_name
  kms_key_name = var.kms_key_name

}

module "storage" {
  source              = "../../modules/storage"
  environment         = var.environment
  app_name            = var.app_name
  kms_key_name        = module.security.kms_key_alias
  test_s3_bucket_name = var.test_s3_bucket_name
  depends_on          = [module.security]
}

module "container" {
  source      = "../../modules/container"
  environment = var.environment
  app_name    = var.app_name
  depends_on  = [module.security, module.networking]
}

module "networking" {
  source      = "../../modules/networking"
  environment = var.environment
  app_name    = var.app_name
}
