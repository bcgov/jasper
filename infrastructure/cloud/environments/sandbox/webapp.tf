module "security" {
  source                      = "../../modules/security"
  environment                 = var.environment
  app_name                    = var.app_name
  kms_key_name                = var.kms_key_name
  ecs_web_task_definition_arn = module.container.ecs_web_task_definition_arn
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
}

module "container" {
  source                              = "../../modules/container"
  environment                         = var.environment
  app_name                            = var.app_name
  region                              = var.region
  ecs_web_task_execution_iam_role_arn = module.security.ecs_web_task_execution_iam_role_arn
  subnet_id                           = module.networking.subnet_id
  ecs_sg_id                           = module.networking.ecs_sg_id
  lb_listener                         = module.networking.lb_listener
  lb_tg_arn                           = module.networking.lb_tg_arn
  ecs_web_log_group_name              = module.monitoring.ecs_web_log_group_name
}

module "monitoring" {
  source      = "../../modules/monitoring"
  environment = var.environment
  app_name    = var.app_name
}
