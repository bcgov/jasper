variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "name" {
  description = "The name/purpose of the SNS topic (e.g., alerts, notifications)"
  type        = string
}

variable "purpose" {
  description = "The purpose description of the SNS topic"
  type        = string
  default     = "Application Notifications"
}

variable "email_addresses" {
  description = "List of email addresses to subscribe to the SNS topic"
  type        = list(string)
  default     = []
}

variable "kms_key_id" {
  description = "The KMS Key ID"
  type        = string
}
