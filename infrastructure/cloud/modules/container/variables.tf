variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "ecs_task_execution_iam_role_arn" {
  description = "ECS Task Execution IAM Role ARN"
}

variable "subnet_id" {
  description = "Subnet ID"
}

variable "ecs_sg_id" {
  description = "ECS Security Group ID"
}

variable "lb_tg_arn" {
  description = "Load Balancer Target Group ARN"
}

variable "lb_listener" {
  description = "Load Balancer Listener"
}

variable "web_image_name" {
  description = "Image Name of the frontend app"
  default     = "jasper-web"
}

variable "api_image_name" {
  description = "Image Name of the backend app"
  default     = "jasper-api"
}

