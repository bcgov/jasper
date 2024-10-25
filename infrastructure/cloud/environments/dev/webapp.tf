module "security" {
  source                   = "../../modules/security"
  environment              = var.environment
  app_name                 = var.app_name
  kms_key_name             = var.kms_key_name
  ecs_web_td_log_group_arn = module.monitoring.ecs_web_td_log_group_arn
  ecs_api_td_log_group_arn = module.monitoring.ecs_api_td_log_group_arn
  ecr_repository_arn       = module.container.ecr_repository_arn
  openshift_iam_user       = var.openshift_iam_user
  iam_user_table_name      = var.iam_user_table_name
  cert_domain_name         = var.cert_domain_name
}

module "storage" {
  source              = "../../modules/storage"
  environment         = var.environment
  app_name            = var.app_name
  kms_key_name        = module.security.kms_key_alias
  test_s3_bucket_name = var.test_s3_bucket_name
  data_sg_id          = module.networking.data_sg_id
  db_username         = module.security.db_username
  db_password         = module.security.db_password
  vpc_id              = var.vpc_id
  kms_key_arn         = module.security.kms_key_arn
  rds_db_ca_cert      = var.rds_db_ca_cert
  depends_on          = [module.security]
}

module "networking" {
  source              = "../../modules/networking"
  environment         = var.environment
  app_name            = var.app_name
  region              = var.region
  vpc_id              = var.vpc_id
  web_subnet_names    = var.web_subnet_names
  app_subnet_names    = var.app_subnet_names
  data_subnet_names   = var.data_subnet_names
  lb_name             = var.lb_name
  default_lb_cert_arn = module.security.default_lb_cert_arn
}

module "container" {
  source                    = "../../modules/container"
  environment               = var.environment
  app_name                  = var.app_name
  region                    = var.region
  ecs_execution_role_arn    = module.security.ecs_execution_role_arn
  web_subnet_ids            = module.networking.web_subnets_ids
  app_subnet_ids            = module.networking.app_subnets_ids
  web_sg_id                 = module.networking.web_sg_id
  app_sg_id                 = module.networking.app_sg_id
  web_tg_arn                = module.networking.web_tg_arn
  api_tg_arn                = module.networking.api_tg_arn
  ecs_web_td_log_group_name = module.monitoring.ecs_web_td_log_group_name
  ecs_api_td_log_group_name = module.monitoring.ecs_api_td_log_group_name
  kms_key_id                = module.security.kms_key_id
  default_lb_dns_name       = module.networking.default_lb_dns_name
  api_secrets               = module.security.api_secrets
  web_secrets               = module.security.web_secrets
  depends_on                = [module.monitoring]
}

module "monitoring" {
  source      = "../../modules/monitoring"
  environment = var.environment
  app_name    = var.app_name
  kms_key_arn = module.security.kms_key_arn
}
