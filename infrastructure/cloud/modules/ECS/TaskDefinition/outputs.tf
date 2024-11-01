output "ecs_td_arn" {
  value = aws_ecs_task_definition.ecs_td.arn
}

output "port" {
  value = var.port
}
