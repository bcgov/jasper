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
  source      = "../../modules/networking"
  environment = var.environment
  app_name    = var.app_name
  region      = var.region
  vpc_cidr    = var.vpc_cidr
  subnet_1    = var.subnet_1
  subnet_2    = var.subnet_2
}

module "container" {
  source                              = "../../modules/container"
  environment                         = var.environment
  app_name                            = var.app_name
  region                              = var.region
  ecs_web_task_execution_iam_role_arn = module.security.ecs_web_task_execution_iam_role_arn
  subnet_1_id                         = module.networking.subnet_1_id
  subnet_2_id                         = module.networking.subnet_2_id
  lb_sg_id                            = module.networking.lb_sg_id
  ecs_sg_id                           = module.networking.ecs_sg_id
  lb_tg_arn                           = module.networking.lb_tg_arn
  ecs_web_log_group_name              = module.monitoring.ecs_web_log_group_name
}

module "monitoring" {
  source      = "../../modules/monitoring"
  environment = var.environment
  app_name    = var.app_name
}
