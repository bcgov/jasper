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

variable "kms_key_arn" {
  description = "The KMS Key ARN"
  type        = string
}

variable "rotate_key_lambda_arn" {
  description = "The Rotate Key Lambda ARN"
  type        = string
}

variable "use_existing_mongo_tls_secret" {
  description = "If true, do not create the mongo TLS secret and instead use an existing one"
  type        = bool
  default     = false
}

variable "existing_mongo_tls_secret_name" {
  description = "Optional existing Mongo TLS secret name to use when use_existing_mongo_tls_secret is true"
  type        = string
  default     = ""
}
