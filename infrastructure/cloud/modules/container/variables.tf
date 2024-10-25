variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "region" {
  description = "The AWS region"
  type        = string
}

variable "ecs_execution_role_arn" {
  description = "ECS Execution Role ARN"
  type        = string
}

variable "web_subnet_ids" {
  description = "The Web Subnet IDs"
  type        = list(string)
}

variable "app_subnet_ids" {
  description = "The App Subnet IDs"
  type        = list(string)
}

variable "web_sg_id" {
  description = "The BCGOV provisioned Web Security Group"
  type        = string
}

variable "app_sg_id" {
  description = "The BCGOV provisioned App Security Group"
  type        = string
}

variable "web_tg_arn" {
  description = "The Web Target Group ARN"
  type        = string
}

variable "api_tg_arn" {
  description = "The API Target Group ARN"
  type        = string
}

variable "ecs_web_td_log_group_name" {
  description = "ECS Web Task Definition Log Group Name in CloudWatch"
  type        = string
}

variable "ecs_api_td_log_group_name" {
  description = "ECS API Task Definition Log Group Name in CloudWatch"
  type        = string
}

variable "kms_key_id" {
  description = "The KMS Key ID"
  type        = string
}

variable "default_lb_dns_name" {
  description = "The BCGov Load Balancer DNS Name"
  type        = string
}

variable "api_secrets" {
  description = "List if env variable secrets used in API"
  type        = list(list(string))
}

variable "web_secrets" {
  description = "List if env variable secrets used in Web"
  type        = list(list(string))
}
