locals {
  alarms = {
    for alarm_config in var.alarm_configurations : alarm_config.name => alarm_config
  }
}

resource "aws_cloudwatch_metric_alarm" "alarm" {
  for_each = local.alarms

  alarm_name          = "${var.app_name}-${var.service_name}-${each.key}-${var.environment}"
  comparison_operator = each.value.comparison_operator
  evaluation_periods  = each.value.evaluation_periods
  metric_name         = each.value.metric_name
  namespace           = each.value.namespace
  period              = each.value.period
  statistic           = each.value.statistic
  threshold           = each.value.threshold
  alarm_description   = each.value.description
  alarm_actions       = each.value.alarm_actions

  dimensions = var.dimensions

  tags = merge(var.tags, {
    Name        = "${var.app_name}-${var.service_name}-${each.key}-${var.environment}"
    Environment = var.environment
    AlarmType   = each.key
  })
}
