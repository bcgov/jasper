variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "kms_key_arn" {
  description = "The custom KMS Key ARN"
  type        = string
}


variable "web_subnet_ids" {
  description = "List of Subnets for Web"
  type        = list(string)
}

variable "app_subnet_ids" {
  description = "List of Subnets for App"
  type        = list(string)
}

variable "app_security_group_id" {
  description = "App Security Group ID"
  type        = string
}

variable "web_security_group_id" {
  description = "Web Security Group ID"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "name" {
  description = "The name identifier for the EFS (e.g., 'coredumps', 'api-dumps')"
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
