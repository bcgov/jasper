variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
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
}

variable "lb_tg_arn" {
  description = "Load Balancer Target Group ARN"
}

variable "web_image_name" {
  description = "Image Name of the Web app"
  type        = string
  default     = "jasper-web"
}

variable "api_image_name" {
  description = "Image Name of the API"
  type        = string
  default     = "jasper-api"
}

variable "web_port" {
  description = "Port Number of the Web app"
  type        = number
  default     = 8080
}

variable "ecs_web_log_group_name" {
  description = "ECS Web Log Group Name in CloudWatch"
  type        = string
}
