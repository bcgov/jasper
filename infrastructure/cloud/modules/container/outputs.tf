output "ecr_url" {
  description = "The ECR URL."
  value       = try(aws_ecr_repository.aws_ecr_repository.repository_url, "")
}
