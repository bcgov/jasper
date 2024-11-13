# This the rest of JASPER's infra resources.
# Make sure that the "initial" stack has been deployed first.

#
# Existing Resources
#
data "aws_caller_identity" "current" {}

# KMS Key
data "aws_kms_key" "kms_key" {
  key_id = "alias/${var.kms_key_name}-${var.environment}"
}

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

# App ECR Repo
data "aws_ecr_repository" "app_ecr_repo" {
  name = "${var.app_name}-app-repo-${var.environment}"
}

# Lambda ECR Repo
data "aws_ecr_repository" "lambda_ecr_repo" {
  name = "${var.app_name}-lambda-repo-${var.environment}"
}

#
# Modules
#

# Create Secrets placeholder for Secrets Manager
module "secrets_manager" {
  source      = "../../modules/SecretsManager"
  environment = var.environment
  app_name    = var.app_name
  region      = var.region
  kms_key_arn = data.aws_kms_key.kms_key.arn
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
  kms_key_arn    = data.aws_kms_key.kms_key.arn
  rds_db_ca_cert = var.rds_db_ca_cert
}

# Create IAM Roles/Policies
module "iam" {
  source              = "../../modules/IAM"
  environment         = var.environment
  app_name            = var.app_name
  kms_key_arn         = data.aws_kms_key.kms_key.arn
  app_ecr_repo_arn    = data.aws_ecr_repository.app_ecr_repo.arn
  openshift_iam_user  = var.openshift_iam_user
  iam_user_table_name = var.iam_user_table_name
  secrets_arn_list    = module.secrets_manager.secrets_arn_list
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

# Create Lambda Functions
module "lambda" {
  source              = "../../modules/Lambda"
  environment         = var.environment
  app_name            = var.app_name
  lambda_role_arn     = module.iam.lambda_role_arn
  apigw_execution_arn = module.apigw.apigw_execution_arn
  lambda_ecr_repo_url = data.aws_ecr_repository.lambda_ecr_repo.repository_url
  mtls_secret_name    = module.secrets_manager.mtls_secret_name
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
  ecr_repository_url     = data.aws_ecr_repository.app_ecr_repo.repository_url
  port                   = 8080
  secret_env_variables   = module.secrets_manager.web_secrets
  kms_key_arn            = data.aws_kms_key.kms_key.arn
}

# Create API ECS Task Definition
module "ecs_api_td" {
  source                 = "../../modules/ECS/TaskDefinition"
  environment            = var.environment
  app_name               = var.app_name
  name                   = "api"
  region                 = var.region
  ecs_execution_role_arn = module.iam.ecs_execution_role_arn
  ecr_repository_url     = data.aws_ecr_repository.app_ecr_repo.repository_url
  port                   = 5000
  env_variables = [
    {
      name  = "CORS_DOMAIN"
      value = module.alb.default_lb_dns_name
    },
    {
      name  = "AWS_API_GATEWAY_URL"
      value = module.apigw.apigw_invoke_url
    }
  ]
  secret_env_variables = module.secrets_manager.api_secrets
  kms_key_arn          = data.aws_kms_key.kms_key.arn
}

# Create Web ECS Service
module "ecs_web_service" {
  source         = "../../modules/ECS/Service"
  environment    = var.environment
  app_name       = var.app_name
  name           = "web"
  ecs_cluster_id = module.ecs_cluster.ecs_cluster_id
  ecs_td_arn     = module.ecs_web_td.ecs_td_arn
  tg_arn         = module.tg_web.tg_arn
  sg_id          = data.aws_security_group.app_sg.id
  subnet_ids     = module.subnets.web_subnets_ids
  port           = module.ecs_web_td.port
}

# Create Api ECS Service
module "ecs_api_service" {
  source         = "../../modules/ECS/Service"
  environment    = var.environment
  app_name       = var.app_name
  name           = "api"
  ecs_cluster_id = module.ecs_cluster.ecs_cluster_id
  ecs_td_arn     = module.ecs_api_td.ecs_td_arn
  tg_arn         = module.tg_api.tg_arn
  sg_id          = data.aws_security_group.app_sg.id
  subnet_ids     = module.subnets.app_subnets_ids
  port           = module.ecs_api_td.port
}
