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
    http_method   = string
    resource_path = string
    env_variables = optional(map(string), {})
    timeout       = optional(number, 300)
    memory_size   = optional(number, 2048)
  }))
  default = {
    # Keys should match the folder name in lambda code
    "get-locations" = {
      http_method   = "GET"
      resource_path = "/locations"
    },
    "get-rooms" = {
      http_method   = "GET"
      resource_path = "/locations/rooms"
    },
    "search-criminal-files" = {
      http_method   = "GET"
      resource_path = "/files/criminal"
    },
    "search-civil-files" = {
      http_method   = "GET"
      resource_path = "/files/civil"
    }
  }
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
