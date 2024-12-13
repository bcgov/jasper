output "kms_key_id" {
  value = module.kms.kms_key_id
}

output "kms_key_arn" {
  value = module.kms.kms_key_arn
}

output "lambda_ecr" {
  value = module.lambda_ecr
}

output "app_ecr" {
  value = module.app_ecr
}
