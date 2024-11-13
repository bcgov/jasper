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

variable "kms_key_arn" {
  description = "The KMS Key ARN"
  type        = string
}

variable "rotate_key_lambda_arn" {
  description = "The Rotate Key Lambda ARN"
  type        = string
}
