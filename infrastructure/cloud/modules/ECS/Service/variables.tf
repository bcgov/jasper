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

variable "ecs_cluster_name" {
  description = "The ECS Cluster Name"
  type        = string
}

variable "min_capacity" {
  description = "Minimum number of tasks"
  type        = number
  default     = 1
}

variable "max_capacity" {
  description = "Maximum number of tasks"
  type        = number
  default     = 10
}

variable "cpu_target_value" {
  description = "Target CPU utilization percentage for autoscaling"
  type        = number
  default     = 70
}

variable "memory_target_value" {
  description = "Target memory utilization percentage for autoscaling"
  type        = number
  default     = 80
}

variable "scale_out_cooldown" {
  description = "Amount of time, in seconds, after a scale out activity completes before another scale out activity can start"
  type        = number
  default     = 300
}

variable "scale_in_cooldown" {
  description = "Amount of time, in seconds, after a scale in activity completes before another scale in activity can start"
  type        = number
  default     = 300
}
