

variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
  default     = "dev"
}

variable "ecr_repository_name" {
  type        = string
  description = "Name of AWS ECR Repository"
  default     = "aws-ecr-repository"
}
