variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "name" {
  description = "The name of ECS cluster"
}

variable "subnet_ids" {
  description = "The Subnet IDs"
  type        = list(string)
}

variable "sg_id" {
  description = "The BCGOV provisioned Security Group"
  type        = string
}

variable "tg_arn" {
  description = "The Target Group ARN"
  type        = string
}

variable "port" {
  description = "The Container port number"
  type        = number
}

variable "ecs_cluster_id" {
  description = "The ECS Cluster Id"
  type        = string
}

variable "ecs_td_arn" {
  description = "The ECS Task Definition ARN"
  type        = string
}
