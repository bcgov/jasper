output "ecr_url" {
  value = try(aws_ecr_repository.ecr_repository.repository_url, "")
}
