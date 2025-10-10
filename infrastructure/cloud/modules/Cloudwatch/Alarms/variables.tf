variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "service_name" {
  description = "The name of the service"
  type        = string
}

variable "dimensions" {
  description = "CloudWatch dimensions for the alarms"
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Additional tags for the alarms"
  type        = map(string)
  default     = {}
}

variable "alarm_configurations" {
  description = "List of alarm configurations"
  type = list(object({
    name                = string
    namespace           = string
    metric_name         = string
    comparison_operator = string
    threshold           = number
    evaluation_periods  = number
    period              = number
    statistic           = string
    description         = string
    alarm_actions       = list(string)
  }))
  default = []
}
