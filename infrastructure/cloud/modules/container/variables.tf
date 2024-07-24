variable "environment" {
  type        = string
  description = "The environment to deploy the application to"
}

variable "ecr_repository_name" {
  type        = string
  description = "Name of AWS ECR Repository"
  default     = "aws_ecr_repository"
}
