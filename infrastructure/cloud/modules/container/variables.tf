variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "ecs_web_task_execution_iam_role_arn" {
  description = "ECS Task Execution IAM Role ARN for Web app"
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
  default     = 80
}
