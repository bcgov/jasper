resource "aws_cloudwatch_log_group" "ecs_web_td_log_group" {
  name              = "/aws/ecs/${var.app_name}-ecs-web-td-log-group-${var.environment}"
  retention_in_days = 90

  kms_key_id = var.kms_key_arn
}

resource "aws_cloudwatch_log_group" "ecs_api_td_log_group" {
  name              = "/aws/ecs/${var.app_name}-ecs-api-td-log-group-${var.environment}"
  retention_in_days = 90

  kms_key_id = var.kms_key_arn
}
