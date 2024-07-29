variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "log_group_retention" {
  description = "The retention period in days for CloudWatch logs"
  type        = number
  default     = 30
}
