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

variable "name" {
  description = "The name of ECS cluster"
}

variable "cpu" {
  description = "The ECS Task Definition CPU"
  type        = number
  default     = 256
}

variable "memory_size" {
  description = "The ECS Task Definition Memory Size"
  type        = number
  default     = 512
}

variable "ecs_execution_role_arn" {
  description = "The ECS Task Definition Execution role ARN"
  type        = string
}

variable "ecr_repository_url" {
  description = "The ECR Repository URL where the image will be referenced"
  type        = string
}

variable "port" {
  description = "The Container port number"
  type        = number
}

variable "env_variables" {
  description = "The environment variables for the container"
  type = list(object({
    name  = string
    value = string
  }))
  default = null
}

variable "secret_env_variables" {
  description = "The sensitive environment variables for the container"
  type        = list(list(string))
}

variable "kms_key_arn" {
  description = "KMS Key ARN"
  type        = string
}

variable "log_group_name" {
  description = "The Cloudwatch Log Group Name"
  type        = string
}
