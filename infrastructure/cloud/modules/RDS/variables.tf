variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "data_sg_id" {
  description = "The Data Security Group Id that will be used by database"
  type        = string
}

variable "db_username" {
  description = "Database username from Secrets Manager"
  type        = string
  sensitive   = true
}

variable "db_password" {
  description = "Database password from Secrets Manager"
  type        = string
  sensitive   = true
}

variable "vpc_id" {
  description = "The provisioned VPC ID"
  type        = string
}

variable "kms_key_arn" {
  description = "The custom KMS Key ARN"
  type        = string
}

variable "rds_db_ca_cert" {
  description = "The Certifiate Authority identifier used in RDS"
  type        = string
}

variable "all_subnet_ids" {
  description = "List of all provisioned subnet ids"
  type        = list(string)
}
