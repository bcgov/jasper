resource "aws_cloudwatch_log_group" "ecs_web_td_log_group" {
  name              = "${var.app_name}-ecs-web-td-log-group-${var.environment}"
  retention_in_days = 90
}
