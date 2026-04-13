variable "account_id" {
  type = string
}

variable "environment" {
  type = string
}

variable "notification_hub_region" {
  type = string
}

variable "email_addresses" {
  type = list(string)
}

variable "managed_notification_sub_categories" {
  type    = list(string)
  default = ["Security", "Operations", "Issue"]
}
