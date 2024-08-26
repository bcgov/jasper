output "ecr_url" {
  value = try(aws_ecr_repository.ecr_repository.repository_url, "")
}

output "ecr_repository_arn" {
  value = aws_ecr_repository.ecr_repository.arn
}
