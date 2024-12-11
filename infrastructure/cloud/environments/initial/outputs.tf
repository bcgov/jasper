output "app_ecr_name" {
  value = module.app_ecr.ecr_name
}

output "lambda_ecr_name" {
  value = module.lambda_ecr.ecr_name
}
