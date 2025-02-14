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

variable "allowed_ip_ranges" {
  description = "Comma delimited list of BC Gov CIDR IP addresses or CIDR ranges"
  type        = string
  sensitive   = true
}

variable "default_lb_arn" {
  description = "The default Load Balancer ARN"
  type        = string
}

variable "log_group_arn" {
  description = "The WAF Log Group ARN"
  type        = string
}
