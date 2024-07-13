

locals {
  environment = "snd"
  application_name = "jasper-aws"
}

module "security" {
  source = "../../modules/security"
  environment = local.environment
    application_name = local.application_name
    kms_key_name = "jasper-kms-key"

}

module "storage" {
  source = "../../modules/storage"
  environment = local.environment
    application_name = local.application_name
    kms_key_name = module.security.kms_key_alias
    test_s3_bucket_name = var.test_s3_bucket_name
    depends_on = [ module.security ]
}