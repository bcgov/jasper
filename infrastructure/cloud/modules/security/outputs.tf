
output "kms_key_alias" {
  value = aws_kms_alias.kms_alias.name
}

output "ecs_execution_role_arn" {
  value = aws_iam_role.ecs_execution_role.arn
}
