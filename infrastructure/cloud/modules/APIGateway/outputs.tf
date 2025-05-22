output "apigw_id" {
  value = aws_api_gateway_rest_api.apigw.id
}

output "apigw_invoke_url" {
  value = aws_api_gateway_stage.apigw_stage.invoke_url
}

output "apigw_execution_arn" {
  value = aws_api_gateway_rest_api.apigw.execution_arn
}
