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

variable "vpc_id" {
  description = "The provisioned VPC ID"
  type        = string
}
