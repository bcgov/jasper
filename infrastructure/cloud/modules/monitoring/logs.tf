resource "aws_cloudwatch_log_group" "ecs_web_log_group" {
  name              = "${var.app_name}-ecs-web-log-group-${var.environment}"
  retention_in_days = 30
}
