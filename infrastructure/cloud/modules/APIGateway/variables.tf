variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "region" {
  description = "The AWS region"
  type        = string
}

variable "account_id" {
  description = "The current AWS Account Id"
  type        = string
}

variable "lambda_functions" {
  description = "Lambda functions config"
  type = map(object({
    http_method   = string
    resource_path = string
    invoke_arn    = string
  }))
}
