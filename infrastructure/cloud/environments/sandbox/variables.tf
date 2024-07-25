variable "test_s3_bucket_name" {
  type        = string
  description = "The name of the S3 bucket to create for testing"
}

variable "region" {
  description = "The AWS region"
  type        = string
  default     = "ca-central-1"
}

variable "kms_key_name" {
  description = "Name of KMS key"
  type        = string
  default     = "jasper-kms-key"
}

variable "app_name" {
  description = "The name of the application"
  type        = string
  default     = "jasper-aws"
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
  default     = "snd"
}
