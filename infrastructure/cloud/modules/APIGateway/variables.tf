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

variable "ecs_execution_role_arn" {
  description = "The ECS Task Definition Execution role ARN"
  type        = string
}

variable "log_group_arn" {
  description = "The API Gateway Cloudwatch Log Group ARN"
  type        = string
}

variable "apigw_logging_role_arn" {
  description = "The API Gateway Logging IAM Role ARN"
  type        = string
}
