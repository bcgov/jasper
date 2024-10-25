variable "kms_key_name" {
  description = "Name of KMS key"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "ecs_web_td_log_group_arn" {
  description = "The ECS Web Task Definition Log Group ARN"
  type        = string
}

variable "ecs_api_td_log_group_arn" {
  description = "The ECS API Task Definition Log Group ARN"
  type        = string
}

variable "ecr_repository_arn" {
  description = "The ECR Repository ARN"
  type        = string
}

variable "openshift_iam_user" {
  description = "Openshift IAM Username"
  type        = string
}

variable "iam_user_table_name" {
  description = "The BCGOV IAM User DynamoDb table name"
  type        = string
}

variable "cert_domain_name" {
  description = "The BCGov provisioned certificate domain name"
  type        = string
}
