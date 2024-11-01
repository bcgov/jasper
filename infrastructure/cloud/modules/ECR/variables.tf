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

variable "repo_name" {
  description = "The ECR Repository name"
  type        = string
}

variable "kms_key_id" {
  description = "The KMS Key ID"
  type        = string
}
