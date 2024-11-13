variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "kms_key_arn" {
  description = "KMS Key ARN"
  type        = string
}

variable "resource_name" {
  description = "Resource name of the Log Group"
  type        = string
}

variable "name" {
  description = "Log Group Name"
  type        = string
}
