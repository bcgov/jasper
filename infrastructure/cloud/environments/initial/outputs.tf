output "app_ecr_name" {
  value = module.app_ecr.aws_ecr_repository.ecr_repository.name
}

output "lambda_ecr_name" {
  value = module.lambda_ecr.aws_ecr_repository.ecr_repository.name
}
