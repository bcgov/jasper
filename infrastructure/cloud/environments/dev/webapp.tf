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

module "networking" {
  source           = "../../modules/networking"
  environment      = var.environment
  app_name         = var.app_name
  region           = var.region
  vpc_id           = var.vpc_id
  web_subnet_names = var.web_subnet_names
  # api_subnet_names = var.api_subnet_names
  # db_subnet_names  = var.db_subnet_names
}

module "container" {
  source                    = "../../modules/container"
  environment               = var.environment
  app_name                  = var.app_name
  region                    = var.region
  ecs_execution_role_arn    = module.security.ecs_execution_role_arn
  subnet_ids                = module.networking.web_subnets_ids
  sg_id                     = module.networking.ecs_sg_id
  lb_tg_arn                 = module.networking.lb_tg_arn
  ecs_web_td_log_group_name = module.monitoring.ecs_web_td_log_group_name
  depends_on                = [module.monitoring]

}

module "monitoring" {
  source      = "../../modules/monitoring"
  environment = var.environment
  app_name    = var.app_name
}
