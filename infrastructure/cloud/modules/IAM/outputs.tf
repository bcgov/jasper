output "ecs_execution_role_arn" {
  value = aws_iam_role.ecs_execution_role.arn
}

output "lambda_role_arn" {
  value = aws_iam_role.lambda_role.arn
}

output "apigw_logging_role_arn" {
  value = aws_iam_role.apigw_logging_role.arn
}
