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

variable "lambda_role_arn" {
  description = "The Lambda IAM Role ARN"
  type        = string
}

# variable "functions" {
#   description = "Lambda functions config"
#   type = map(object({
#     http_method         = string
#     resource_path       = string
#     env_variables       = optional(map(string), {})
#     timeout             = optional(number, null)
#     memory_size         = optional(number, null)
#     statement_id_prefix = optional(string, "AllowAPIGatewayInvoke")
#     principal           = optional(string, "apigateway.amazonaws.com")
#     source_arn          = optional(string, null)
#     enable_vpc_config   = optional(bool, true)
#   }))
#   default = {}
# }

variable "apigw_execution_arn" {
  description = "The API Gateway Execution ARN"
  type        = string
}

variable "lambda_ecr_repo_url" {
  description = "The Lambda ECR Repository URL"
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

variable "subnet_ids" {
  description = "The Subnet IDs"
  type        = list(string)
}

variable "sg_ids" {
  description = "The Security Group IDs"
  type        = list(string)
}

variable "lambda_secrets" {
  description = "List of secrets used by Lambda functions"
  type        = map(string)
}

variable "ecs_cluster_name" {
  description = "ECS Cluster Name"
  type        = string
}

variable "efs_access_point_arn" {
  description = "EFS Access Point ARN for Lambda file system mount"
  type        = string
}

variable "efs_mount_path" {
  description = "Local mount path for EFS in Lambda"
  type        = string
}

variable "get_assigned_cases_lambda_timeout" {
  description = "Timeout for getAssignedCases Lambda function"
  type        = number
}