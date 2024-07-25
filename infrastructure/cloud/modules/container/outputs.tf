output "ecr_url" {
  description = "The ECR URL."
  value       = try(aws_ecr_repository.ecr_repository.repository_url, "")
}
