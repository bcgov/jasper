variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The AWS environment to deploy to"
  type        = string
}

variable "lb_name" {
  description = "The BCGOV provisioned Load Balancer name"
  type        = string
}

variable "cert_domain_name" {
  description = "The BCGov provisioned certificate domain name"
  type        = string
}

variable "tg_web_arn" {
  description = "The Web Target Group ARN"
  type        = string
}

variable "tg_api_arn" {
  description = "The API Target Group ARN"
  type        = string
}

variable "web_security_group_id" {
  description = "The ID of the security group for the web tier"
  type        = string
}

variable "vpc_id" {
  description = "The ID of the VPC"
  type        = string
}

variable "web_subnets_ids" {
  description = "The IDs of the subnets for the web tier"
  type        = list(string)
}

variable "account_id" {
  description = "The current AWS Account Id"
  type        = string
}

variable "region" {
  description = "The AWS region"
  type        = string
}

variable "lza_log_archive_account_id" {
  description = "LZA Log Archive Account ID for centralized ALB logging"
  type        = string
  default     = "897722703828"
}
