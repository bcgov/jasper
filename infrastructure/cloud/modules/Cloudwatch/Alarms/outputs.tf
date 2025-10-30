output "alarm_arns" {
  description = "Map of alarm names to their ARNs"
  value       = { for k, v in aws_cloudwatch_metric_alarm.alarm : k => v.arn }
}

output "alarm_names" {
  description = "Map of alarm keys to their full alarm names"
  value       = { for k, v in aws_cloudwatch_metric_alarm.alarm : k => v.alarm_name }
}
