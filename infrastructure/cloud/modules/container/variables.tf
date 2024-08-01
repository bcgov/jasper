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
  description = "Public Subnet IDs"
  type        = list(string)
}

variable "sg_id" {
  description = "Load Balancer Security Group ID"
  type        = string
}

variable "lb_tg_arn" {
  description = "Load Balancer Target Group ARN"
  type        = string
}

variable "ecs_web_log_group_name" {
  description = "ECS Web Log Group Name in CloudWatch"
  type        = string
}
