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

variable "subnet_private_id" {
  description = "Private Subnet ID"
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
