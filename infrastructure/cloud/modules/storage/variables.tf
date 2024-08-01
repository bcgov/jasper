
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
