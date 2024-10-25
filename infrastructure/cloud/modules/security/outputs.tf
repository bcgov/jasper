
output "kms_key_alias" {
  value = aws_kms_alias.kms_alias.name
}

output "ecs_execution_role_arn" {
  value = aws_iam_role.ecs_execution_role.arn
}

output "kms_key_id" {
  value = aws_kms_key.kms_key.key_id
}

output "kms_key_arn" {
  value = aws_kms_key.kms_key.arn
}
