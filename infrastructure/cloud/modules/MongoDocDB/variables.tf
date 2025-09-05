

variable "environment" {
  description = "The environment for the resources"
  type        = string
}

variable "data_subnets_ids" {
  description = "List of subnet IDs for the DocDB subnet group"
  type        = list(string)
}

variable "kms_key_id" {
  description = "KMS Key ID for encrypting the DocDB storage"
  type        = string
}

variable "app_sg_id" {
  description = "Security Group ID for the DocDB cluster"
  type        = string
}

variable "delete_protection_enabled" {
  description = "Flag to enable or disable deletion protection for the DocDB cluster"
  type        = bool
  default     = true
}

variable mongo_node_count {
  description = "Number of instances in the DocDB cluster"
  type        = number
  default     = 1
}

variable mongo_instance_type {
  description = "Instance type for the DocDB instances"
  type        = string
  default     = "db.t3.medium"
}

variable mongousername {
  description = "Username for the MongoDB admin user"
  type        = string
}

variable app_name {
  description = "The application name for the resources"
  type        = string
}