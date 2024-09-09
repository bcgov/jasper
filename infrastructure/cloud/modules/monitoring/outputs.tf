output "ecs_web_td_log_group_name" {
  value = aws_cloudwatch_log_group.ecs_web_td_log_group.name
}

output "ecs_web_td_log_group_arn" {
  value = aws_cloudwatch_log_group.ecs_web_td_log_group.arn
}

output "ecs_api_td_log_group_name" {
  value = aws_cloudwatch_log_group.ecs_api_td_log_group.name
}

output "ecs_api_td_log_group_arn" {
  value = aws_cloudwatch_log_group.ecs_api_td_log_group.arn
}
