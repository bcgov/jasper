locals {
  lambda_functions = {
    for k, v in var.functions : k => {
      name          = k
      memory_size   = lookup(v, "memory_size", 2048)
      timeout       = lookup(v, "timeout", 30)
      http_method   = v.http_method
      resource_path = v.resource_path
      env_variables = v.env_variables
    }
  }

  default_env_variables = {
    MTLS_SECRET_NAME = var.mtls_secret_name
  }
}

resource "aws_lambda_function" "lambda" {
  for_each = local.lambda_functions

  function_name = "${var.app_name}-${each.key}-lambda-${var.environment}"
  role          = var.lambda_role_arn
  timeout       = each.value.timeout
  memory_size   = each.value.memory_size
  package_type  = "Image"
  image_uri     = "${var.lambda_ecr_repo_url}:dummy-image" # This is a placeholder image and will be replaced every deployment of GHA.

  environment {
    variables = merge(local.default_env_variables, each.value.env_variables)
  }

  lifecycle {
    create_before_destroy = true
    ignore_changes        = [image_uri]
  }

  tracing_config {
    mode = "Active"
  }
}

resource "aws_lambda_permission" "lambda_permissions" {
  for_each = local.lambda_functions

  statement_id  = "AllowAPIGatewayInvoke-${each.key}"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.lambda[each.key].arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${var.apigw_execution_arn}/*/${each.value.http_method}${each.value.resource_path}"
}
