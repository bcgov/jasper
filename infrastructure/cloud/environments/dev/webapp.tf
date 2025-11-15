#
# "initial" stack containing resources that main stack depends on (e.g. ECR, KMS, openshiftuser)
#
module "initial" {
  source              = "../../modules/initial"
  openshift_iam_user  = var.openshift_iam_user
  iam_user_table_name = var.iam_user_table_name
  test_s3_bucket_name = var.test_s3_bucket_name
  region              = var.region
  kms_key_name        = var.kms_key_name
  app_name            = var.app_name
  environment         = var.environment
}

#
# The "main" stack 
#
data "aws_caller_identity" "current" {}

# VPC
data "aws_vpc" "vpc" {
  id = var.vpc_id
}

# Security Groups
data "aws_security_group" "web_sg" {
  name = "Web_sg"
}

data "aws_security_group" "app_sg" {
  name = "App_sg"
}

data "aws_security_group" "data_sg" {
  name = "Data_sg"
}

#
# Modules
#

# Create Secrets placeholder for Secrets Manager
module "secrets_manager" {
  source                = "../../modules/SecretsManager"
  environment           = var.environment
  app_name              = var.app_name
  region                = var.region
  kms_key_arn           = module.initial.kms_key_arn
  rotate_key_lambda_arn = module.lambda.lambda_functions["rotate-key"].arn
}

# Create RDS Database
module "rds" {
  source         = "../../modules/RDS"
  environment    = var.environment
  app_name       = var.app_name
  db_username    = module.secrets_manager.db_username
  db_password    = module.secrets_manager.db_password
  data_sg_id     = data.aws_security_group.data_sg.id
  vpc_id         = data.aws_vpc.vpc.id
  kms_key_arn    = module.initial.kms_key_arn
  rds_db_ca_cert = var.rds_db_ca_cert
  all_subnet_ids = module.subnets.all_subnet_ids
}

module "mongodb" {
  source                    = "../../modules/MongoDocDB"
  environment               = var.environment
  data_subnets_ids          = module.subnets.data_subnets_ids
  kms_key_id                = module.initial.kms_key_arn
  app_sg_id                 = data.aws_security_group.app_sg.id
  delete_protection_enabled = var.delete_protection_enabled
  mongo_node_count          = var.mongo_node_count
  mongo_instance_type       = var.mongo_instance_type
  mongousername             = var.mongousername
  app_name                  = var.app_name
}

# Create IAM Roles/Policies
module "iam" {
  source              = "../../modules/IAM"
  environment         = var.environment
  app_name            = var.app_name
  kms_key_arn         = module.initial.kms_key_arn
  app_ecr_repo_arn    = module.initial.app_ecr.ecr_repo_arn
  openshift_iam_user  = var.openshift_iam_user
  iam_user_table_name = var.iam_user_table_name
  secrets_arn_list    = module.secrets_manager.secrets_arn_list
  account_id          = data.aws_caller_identity.current.account_id
  kms_key_id          = module.initial.kms_key_arn
  region              = var.region
  vpc_id              = data.aws_vpc.vpc.id
}

# Parse Subnets
module "subnets" {
  source            = "../../modules/Subnets"
  web_subnet_names  = var.web_subnet_names
  app_subnet_names  = var.app_subnet_names
  data_subnet_names = var.data_subnet_names
  vpc_id            = data.aws_vpc.vpc.id
}

# Create Target Groups
module "tg_web" {
  source            = "../../modules/TargetGroup"
  environment       = var.environment
  app_name          = var.app_name
  name              = "web"
  port              = 8080
  health_check_path = "/"
  vpc_id            = data.aws_vpc.vpc.id
  protocol          = "HTTPS"
}

module "tg_api" {
  source            = "../../modules/TargetGroup"
  environment       = var.environment
  app_name          = var.app_name
  name              = "api"
  port              = 5000
  health_check_path = "/api/test/headers"
  vpc_id            = data.aws_vpc.vpc.id
  protocol          = "HTTP"
}

# Setup ALB Listeners
module "alb" {
  source           = "../../modules/ALB"
  environment      = var.environment
  app_name         = var.app_name
  lb_name          = var.lb_name
  cert_domain_name = var.cert_domain_name
  tg_web_arn       = module.tg_web.tg_arn
  tg_api_arn       = module.tg_api.tg_arn
}

# Setup EFS Files storage
module "efs_files" {
  source             = "../../modules/EFS"
  environment        = var.environment
  app_name           = var.app_name
  name               = var.efs_config.files_dir
  purpose            = "Temporary file storage when accessing court files."
  subnet_ids         = module.subnets.app_subnets_ids
  security_group_ids = [data.aws_security_group.app_sg.id]
  kms_key_arn        = module.initial.kms_key_arn
}

# Create Lambda Functions
module "lambda" {
  source               = "../../modules/Lambda"
  environment          = var.environment
  app_name             = var.app_name
  lambda_role_arn      = module.iam.lambda_role_arn
  apigw_execution_arn  = module.apigw.apigw_execution_arn
  lambda_ecr_repo_url  = module.initial.lambda_ecr.ecr_repo_url
  lambda_memory_size   = var.lambda_memory_size
  subnet_ids           = module.subnets.all_subnet_ids
  sg_ids               = [data.aws_security_group.web_sg.id, data.aws_security_group.data_sg.id, data.aws_security_group.app_sg.id]
  lambda_secrets       = module.secrets_manager.lambda_secrets
  ecs_cluster_name     = module.ecs_cluster.ecs_cluster.name
  efs_access_point_arn = module.efs_files.access_point_arn
  efs_mount_path       = var.efs_config.mount_path
  region               = var.region
  account_id           = data.aws_caller_identity.current.account_id
}

# Create Cloudwatch LogGroups
module "ecs_api_td_log_group" {
  source        = "../../modules/Cloudwatch/LogGroup"
  environment   = var.environment
  app_name      = var.app_name
  kms_key_arn   = module.initial.kms_key_arn
  resource_name = "ecs"
  name          = "api-td"
}

module "ecs_web_td_log_group" {
  source        = "../../modules/Cloudwatch/LogGroup"
  environment   = var.environment
  app_name      = var.app_name
  kms_key_arn   = module.initial.kms_key_arn
  resource_name = "ecs"
  name          = "web-td"
}

module "apigw_api_log_group" {
  source        = "../../modules/Cloudwatch/LogGroup"
  environment   = var.environment
  app_name      = var.app_name
  kms_key_arn   = module.initial.kms_key_arn
  resource_name = "apigateway"
  name          = "api"
}

module "waf_log_group" {
  source                  = "../../modules/Cloudwatch/LogGroup"
  environment             = var.environment
  app_name                = var.app_name
  kms_key_arn             = module.initial.kms_key_arn
  resource_name           = "waf"
  name                    = "web"
  log_group_name_override = "aws-waf-logs-${var.app_name}-${var.environment}"
}

# Create API Gateway
module "apigw" {
  source                 = "../../modules/APIGateway"
  environment            = var.environment
  app_name               = var.app_name
  region                 = var.region
  account_id             = data.aws_caller_identity.current.account_id
  lambda_functions       = module.lambda.lambda_functions
  ecs_execution_role_arn = module.iam.ecs_execution_role_arn
  log_group_arn          = module.apigw_api_log_group.log_group.arn
  apigw_logging_role_arn = module.iam.apigw_logging_role_arn
}

# Create ECS Cluster
module "ecs_cluster" {
  source      = "../../modules/ECS/Cluster"
  environment = var.environment
  app_name    = var.app_name
  name        = "app"
}

# Create Web ECS Task Definition
module "ecs_web_td" {
  source                 = "../../modules/ECS/TaskDefinition"
  environment            = var.environment
  app_name               = var.app_name
  name                   = "web"
  region                 = var.region
  ecs_execution_role_arn = module.iam.ecs_execution_role_arn
  ecr_repository_url     = module.initial.app_ecr.ecr_repo_url
  port                   = 8080
  secret_env_variables   = module.secrets_manager.web_secrets
  kms_key_arn            = module.initial.kms_key_arn
  log_group_name         = module.ecs_web_td_log_group.log_group.name
  cpu                    = var.web_ecs_config.cpu
  memory_size            = var.web_ecs_config.memory_size
}

# SNS Topic for ECS Alerts
module "sns_ecs_alerts" {
  source          = "../../modules/SNS"
  environment     = var.environment
  app_name        = var.app_name
  name            = "ecs-alerts"
  purpose         = "ECS CloudWatch Alarm Notifications"
  email_addresses = var.alarm_recipients
  kms_key_id      = module.initial.kms_key_arn
}

# Create API ECS Task Definition
module "ecs_api_td" {
  source                 = "../../modules/ECS/TaskDefinition"
  environment            = var.environment
  app_name               = var.app_name
  name                   = "api"
  region                 = var.region
  ecs_execution_role_arn = module.iam.ecs_execution_role_arn
  ecr_repository_url     = module.initial.app_ecr.ecr_repo_url
  port                   = 5000
  env_variables = [
    {
      name  = "CORS_DOMAIN"
      value = module.alb.default_lb_dns_name
    },
    {
      name  = "AWS_API_GATEWAY_URL"
      value = "${module.apigw.apigw_invoke_url}"
    },
    {
      name  = "DEFAULT_USERS"
      value = "${module.secrets_manager.default_users}"
    },
    {
      name  = "AWS_REGION"
      value = var.region
    },
    {
      name  = "AWS_GET_ASSIGNED_CASES_LAMBDA_NAME"
      value = "${module.lambda.lambda_functions["get-assigned-cases-request"].name}"
    }
  ]
  secret_env_variables = module.secrets_manager.api_secrets
  kms_key_arn          = module.initial.kms_key_arn
  log_group_name       = module.ecs_api_td_log_group.log_group.name
  cpu                  = var.api_ecs_config.cpu
  memory_size          = var.api_ecs_config.memory_size
  efs_volume_config = {
    name            = "efs-files-storage"
    file_system_id  = module.efs_files.efs_id
    access_point_id = module.efs_files.access_point_id
    root_directory  = var.efs_config.files_dir
    container_path  = var.efs_config.mount_path
  }
}

# Create Web ECS Service
module "ecs_web_service" {
  source           = "../../modules/ECS/Service"
  environment      = var.environment
  app_name         = var.app_name
  name             = "web"
  ecs_cluster_id   = module.ecs_cluster.ecs_cluster.id
  ecs_td_arn       = module.ecs_web_td.ecs_td_arn
  tg_arn           = module.tg_web.tg_arn
  sg_id            = data.aws_security_group.app_sg.id
  subnet_ids       = module.subnets.web_subnets_ids
  port             = module.ecs_web_td.port
  ecs_cluster_name = module.ecs_cluster.ecs_cluster.name
  min_capacity     = var.web_ecs_config.min_capacity
  max_capacity     = var.web_ecs_config.max_capacity
}

# Create CloudWatch Alarms for Web ECS Service
module "ecs_web_alarms" {
  source       = "../../modules/Cloudwatch/Alarms"
  environment  = var.environment
  app_name     = var.app_name
  service_name = "web-ecs-service"
  dimensions = {
    ClusterName = module.ecs_cluster.ecs_cluster.name
    ServiceName = module.ecs_web_service.service_name
  }
  alarm_configurations = [
    {
      name                = "cpu-high"
      namespace           = "AWS/ECS"
      metric_name         = "CPUUtilization"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.cpu_threshold
      evaluation_periods  = var.alarm_config.evaluation_periods
      period              = var.alarm_config.period
      statistic           = "Average"
      description         = "CPU utilization sustained above ${var.alarm_config.cpu_threshold}% for ${var.alarm_config.evaluation_periods} minutes"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "memory-high"
      namespace           = "AWS/ECS"
      metric_name         = "MemoryUtilization"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.memory_threshold
      evaluation_periods  = var.alarm_config.evaluation_periods
      period              = var.alarm_config.period
      statistic           = "Average"
      description         = "Memory utilization above ${var.alarm_config.memory_threshold}% for ${var.alarm_config.evaluation_periods} minutes"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "task-count-high"
      namespace           = "ECS/ContainerInsights"
      metric_name         = "RunningTaskCount"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.task_count_high_threshold
      evaluation_periods  = var.alarm_config.task_evaluation_periods
      period              = var.alarm_config.task_period
      statistic           = "Average"
      description         = "Web service has scaled up."
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "task-count-zero"
      namespace           = "ECS/ContainerInsights"
      metric_name         = "RunningTaskCount"
      comparison_operator = "LessThanThreshold"
      threshold           = var.alarm_config.task_count_low_threshold
      evaluation_periods  = var.alarm_config.task_evaluation_periods
      period              = var.alarm_config.task_period
      statistic           = "Minimum"
      description         = "CRITICAL: Web service has no running tasks - service is down"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    }
  ]
}

# Create Api ECS Service
module "ecs_api_service" {
  source           = "../../modules/ECS/Service"
  environment      = var.environment
  app_name         = var.app_name
  name             = "api"
  ecs_cluster_id   = module.ecs_cluster.ecs_cluster.id
  ecs_td_arn       = module.ecs_api_td.ecs_td_arn
  tg_arn           = module.tg_api.tg_arn
  sg_id            = data.aws_security_group.app_sg.id
  subnet_ids       = module.subnets.app_subnets_ids
  port             = module.ecs_api_td.port
  ecs_cluster_name = module.ecs_cluster.ecs_cluster.name
  min_capacity     = var.api_ecs_config.min_capacity
  max_capacity     = var.api_ecs_config.max_capacity
}

# Create CloudWatch Alarms for Api ECS Service
module "ecs_api_alarms" {
  source       = "../../modules/Cloudwatch/Alarms"
  environment  = var.environment
  app_name     = var.app_name
  service_name = "api-ecs-service"
  dimensions = {
    ClusterName = module.ecs_cluster.ecs_cluster.name
    ServiceName = module.ecs_api_service.service_name
  }
  alarm_configurations = [
    {
      name                = "cpu-high"
      namespace           = "AWS/ECS"
      metric_name         = "CPUUtilization"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.cpu_threshold
      evaluation_periods  = var.alarm_config.evaluation_periods
      period              = var.alarm_config.period
      statistic           = "Average"
      description         = "CPU utilization sustained above ${var.alarm_config.cpu_threshold}% for ${var.alarm_config.evaluation_periods} minutes"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "memory-high"
      namespace           = "AWS/ECS"
      metric_name         = "MemoryUtilization"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.memory_threshold
      evaluation_periods  = var.alarm_config.evaluation_periods
      period              = var.alarm_config.period
      statistic           = "Average"
      description         = "Memory utilization above ${var.alarm_config.memory_threshold}% for ${var.alarm_config.evaluation_periods} minutes"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "task-count-high"
      namespace           = "ECS/ContainerInsights"
      metric_name         = "RunningTaskCount"
      comparison_operator = "GreaterThanThreshold"
      threshold           = var.alarm_config.task_count_high_threshold
      evaluation_periods  = var.alarm_config.task_evaluation_periods
      period              = var.alarm_config.task_period
      statistic           = "Average"
      description         = "API service has scaled up."
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    },
    {
      name                = "task-count-zero"
      namespace           = "ECS/ContainerInsights"
      metric_name         = "RunningTaskCount"
      comparison_operator = "LessThanThreshold"
      threshold           = var.alarm_config.task_count_low_threshold
      evaluation_periods  = var.alarm_config.task_evaluation_periods
      period              = var.alarm_config.task_period
      statistic           = "Minimum"
      description         = "CRITICAL: API service has no running tasks - service is down"
      alarm_actions       = [module.sns_ecs_alerts.sns_topic_arn]
    }
  ]
}

# WAF
module "waf" {
  source            = "../../modules/WAF"
  environment       = var.environment
  app_name          = var.app_name
  region            = var.region
  allowed_ip_ranges = module.secrets_manager.allowed_ip_ranges
  default_lb_arn    = module.alb.default_lb_arn
  log_group_arn     = module.waf_log_group.log_group.arn
}
