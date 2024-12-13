output "ecr_repo_arn" {
  value = aws_ecr_repository.ecr_repository.arn
}

output "ecr_repo_url" {
  value = aws_ecr_repository.ecr_repository.repository_url
}

output "ecr_name" {
  value = aws_ecr_repository.ecr_repository.name
}
