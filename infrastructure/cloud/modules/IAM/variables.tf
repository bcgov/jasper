variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "kms_key_arn" {
  description = "The KMS Key ARN"
  type        = string
}

variable "app_ecr_repo_arn" {
  description = "The App (UI/API) ECR Repository ARN"
  type        = string
}

variable "openshift_iam_user" {
  description = "Openshift IAM Username"
  type        = string
}

variable "iam_user_table_name" {
  description = "The BCGOV IAM User DynamoDb table name"
  type        = string
}

variable "secrets_arn_list" {
  description = "List of Secrets ARN"
  type        = list(string)
}

variable "account_id" {
  description = "The current AWS Account Id"
  type        = string
}

variable "kms_key_id" {
  description = "The custom KMS Key Id"
  type        = string
}

variable "region" {
  description = "The AWS region"
  type        = string
}
