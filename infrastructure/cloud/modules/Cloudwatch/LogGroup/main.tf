resource "aws_cloudwatch_log_group" "log_group" {
  name              = "/aws/${var.resource_name}/${var.app_name}-${var.resource_name}-${var.name}-log-group-${var.environment}"
  retention_in_days = 90
  kms_key_id        = var.kms_key_arn
}

