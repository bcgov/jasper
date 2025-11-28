variable "environment" {
  description = "The environment to deploy to"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "name" {
  description = "The name identifier for the EFS (e.g., 'coredumps', 'api-dumps', 'files')"
  type        = string
}

variable "purpose" {
  description = "The purpose of this EFS file system"
  type        = string
  default     = "Persistent storage"
}

variable "subnet_ids" {
  description = "List of subnet IDs for EFS mount targets"
  type        = list(string)
}

variable "security_group_ids" {
  description = "List of existing security group IDs to attach to EFS mount targets"
  type        = list(string)
}

variable "kms_key_arn" {
  description = "KMS key ARN for EFS encryption"
  type        = string
}