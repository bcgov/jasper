
variable test_s3_bucket_name {
  type = string
  description = "The name of the S3 bucket to create for testing"
}

variable application_name {
  type = string
  description = "The name of the application"
  default = "bcgov-jasper-aws-bootstrap"
}

variable environment {
  type = string
  description = "The environment to deploy the application to"
  default = "dev"
}

variable kms_key_name {
  type = string
  description = "The name of the KMS key"
  default = "jasper-kms-key"
}