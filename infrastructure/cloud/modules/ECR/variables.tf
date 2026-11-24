variable "environment" {
  description = "The environment to deploy the application to"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "region" {
  description = "The AWS region"
  type        = string
}

variable "repo_name" {
  description = "The ECR Repository name"
  type        = string
}

variable "kms_key_id" {
  description = "The KMS Key ID"
  type        = string
}

variable "tagged_image_patterns" {
  description = "Image tag patterns that each retain the 10 most recently pushed matching images."
  type        = set(string)
  nullable    = false

  validation {
    condition     = length(var.tagged_image_patterns) > 0
    error_message = "At least one image tag pattern must be supplied."
  }

  validation {
    condition = alltrue([
      for pattern in var.tagged_image_patterns : try(
        can(regex("^[A-Za-z0-9_.*-]+$", pattern)) &&
        can(regex("[A-Za-z0-9_]", pattern)) &&
        length(regexall("\\*", pattern)) <= 4,
        false
      )
    ])
    error_message = "Patterns must be non-null, nonempty, use only letters, digits, underscores, dots, hyphens and up to four wildcards (*), and contain a letter, digit or underscore to avoid catch-all expiration."
  }
}
