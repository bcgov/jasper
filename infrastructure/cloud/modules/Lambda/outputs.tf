output "lambda_functions" {
  value = {
    for name, lambda in aws_lambda_function.lambda : name => {
      name          = lambda.function_name
      http_method   = local.lambda_functions[name].http_method
      resource_path = local.lambda_functions[name].resource_path
      invoke_arn    = lambda.invoke_arn
      arn           = lambda.arn
    }
  }
}
