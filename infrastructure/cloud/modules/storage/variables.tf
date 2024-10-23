
variable "test_s3_bucket_name" {
  type        = string
  description = "The name of the S3 bucket to create for testing"
}

variable "kms_key_name" {
  description = "Name of KMS key"
  type        = string
}

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
