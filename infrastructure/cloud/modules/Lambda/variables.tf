variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "lambda_role_arn" {
  description = "The Lambda IAM Role ARN"
  type        = string
}

variable "functions" {
  description = "Lambda functions config"
  type = map(object({
    http_method         = string
    resource_path       = string
    env_variables       = optional(map(string), {})
    timeout             = optional(number, null)
    memory_size         = optional(number, null)
    statement_id_prefix = optional(string, "AllowAPIGatewayInvoke")
    principal           = optional(string, "apigateway.amazonaws.com")
    source_arn          = optional(string, null)
  }))
  default = {}
}

variable "apigw_execution_arn" {
  description = "The API Gateway Execution ARN"
  type        = string
}

variable "lambda_ecr_repo_url" {
  description = "The Lambda ECR Repository URL"
  type        = string
}

variable "mtls_secret_name" {
  description = "The secret name of mTLS Cert in Secrets Manager"
  type        = string
}

variable "lambda_memory_size" {
  description = "The Lambda Function default Memory Size"
  type        = number
}

variable "lambda_timeout" {
  description = "The Lambda Fucntion default timeout"
  type        = number
  default     = 30
}
