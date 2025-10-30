variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "kms_key_arn" {
  description = "The custom KMS Key ARN"
  type        = string
}


variable "web_subnet_ids" {
  description = "List of Subnets for Web"
  type        = list(string)
}

variable "app_subnet_ids" {
  description = "List of Subnets for App"
  type        = list(string)
}

variable "app_security_group_id" {
  description = "App Security Group ID"
  type        = string
}

variable "web_security_group_id" {
  description = "Web Security Group ID"
  type        = string
}
