variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "name" {
  description = "The name of Target Group"
  type        = string
}

variable "port" {
  description = "The port number"
  type        = number
}

variable "health_check_path" {
  description = "The health check path"
  type        = string
}

variable "vpc_id" {
  description = "The VPC Id"
  type        = string
}

variable "protocol" {
  description = "The protocol that will be used by the Target Group and Health Check"
  type        = string
}
