variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "region" {
  description = "The AWS region"
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

# variable "api_subnet_names" {
#   description = "List of Subnets for API"
#   type        = list(string)
# }

# variable "db_subnet_names" {
#   description = "List of Subnets for Database"
#   type        = list(string)
# }
