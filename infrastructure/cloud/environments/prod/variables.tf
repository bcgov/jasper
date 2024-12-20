variable "test_s3_bucket_name" {
  description = "The name of the S3 bucket to create for testing"
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

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "vpc_id" {
  description = "The provisioned VPC ID"
  type        = string
}

variable "web_subnet_names" {
  description = "List of Subnets for Web"
  type        = list(string)
}

variable "app_subnet_names" {
  description = "List of Subnets for App"
  type        = list(string)
}

variable "data_subnet_names" {
  description = "List of Subnets for Data"
  type        = list(string)
}

variable "openshift_iam_user" {
  description = "Openshift IAM Username"
  type        = string
}

variable "iam_user_table_name" {
  description = "The BCGOV DynamoDb IAM user table"
  type        = string
}

variable "lb_name" {
  description = "The BCGOV provisioned Load Balancer name"
  type        = string
}

variable "rds_db_ca_cert" {
  description = "The Certifiate Authority identifier used in RDS"
  type        = string
}

variable "cert_domain_name" {
  description = "The BCGov provisioned certificate domain name"
  type        = string
}

variable "lambda_memory_size" {
  description = "The Lambda Function default Memory Size"
  type        = number
}
