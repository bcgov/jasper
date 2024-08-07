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
<<<<<<< HEAD
=======

variable "subnet_ids" {
  description = "The default VPC subnet ids"
  type        = list(string)
}

>>>>>>> master
