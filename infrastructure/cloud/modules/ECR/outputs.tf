output "ecs_cluster_arn" {
  value = aws_ecr_repository.ecr_repository.arn
}

output "ecr_name" {
  value = aws_ecr_repository.ecr_repository.name
}
