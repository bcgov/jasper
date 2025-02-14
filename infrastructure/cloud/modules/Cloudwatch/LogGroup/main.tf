resource "aws_cloudwatch_log_group" "log_group" {
  name              = var.log_group_name_override != null ? var.log_group_name_override : "/aws/${var.resource_name}/${var.app_name}-${var.resource_name}-${var.name}-log-group-${var.environment}"
  retention_in_days = 90
  kms_key_id        = var.kms_key_arn
}

