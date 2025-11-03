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


variable "delete_protection_enabled" {
  description = "Flag to enable or disable deletion protection for the DocDB cluster"
  type        = bool
  default     = true
}

variable "mongo_node_count" {
  description = "Number of instances in the DocDB cluster"
  type        = number
  default     = 1
}

variable "mongo_instance_type" {
  description = "Instance type for the DocDB instances"
  type        = string
  default     = "db.t3.medium"
}

variable "mongousername" {
  description = "Username for the MongoDB admin user"
  type        = string
}

variable "alarm_recipients" {
  description = "List of email addresses to receive alarm notifications"
  type        = list(string)
}

variable "alarm_config" {
  description = "CloudWatch alarm configuration"
  type = object({
    cpu_threshold             = number
    memory_threshold          = number
    evaluation_periods        = number
    period                    = number
    task_count_low_threshold  = number
    task_count_high_threshold = number
    task_evaluation_periods   = number
    task_period               = number
  })
}

variable "web_ecs_config" {
  description = "ECS configuration for the web service"
  type = object({
    min_capacity = number
    max_capacity = number
    cpu          = number
    memory_size  = number
  })
}

variable "api_ecs_config" {
  description = "ECS configuration for the API service"
  type = object({
    min_capacity = number
    max_capacity = number
    cpu          = number
    memory_size  = number
  })
}

variable "efs_config" {
  description = "EFS configuration"
  type = object({
    mount_path = string
    files_dir  = string
  })
}
