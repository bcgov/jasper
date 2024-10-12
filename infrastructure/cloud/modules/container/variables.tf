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

variable "subnet_ids" {
  description = "Subnet IDs in which ECS will deploy the tasks"
  type        = list(string)
}

variable "ecs_sg_id" {
  description = "Load Balancer Security Group ID"
  type        = string
}

variable "lb_tg_arn" {
  description = "Load Balancer Target Group ARN"
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

