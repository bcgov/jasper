output "ecr_url" {
  value = try(aws_ecr_repository.ecr_repository.repository_url, "")
}

output "ecr_repository_arn" {
  value = aws_ecr_repository.ecr_repository.arn
}

output "ecs_web_td_arn" {
  value = aws_ecs_task_definition.ecs_web_task_definition.arn
}
