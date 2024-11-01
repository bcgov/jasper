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

variable "kms_key_name" {
  description = "Name of KMS key"
  type        = string
}

variable "openshift_iam_user" {
  description = "Openshift IAM Username"
  type        = string
}

variable "account_id" {
  description = "The current AWS Account Id"
  type        = string
}
