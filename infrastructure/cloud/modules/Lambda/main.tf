locals {
  # Keys should match the folder name in lambda code
  default_functions = {
    "get-locations" = {
      http_method   = "GET"
      resource_path = "/locations"
    },
    "get-rooms" = {
      http_method   = "GET"
      resource_path = "/locations/rooms"
    },
    "search-criminal-files" = {
      http_method   = "GET"
      resource_path = "/files/criminal"
    },
    "search-civil-files" = {
      http_method   = "GET"
      resource_path = "/files/civil"
    }
  }

  lambda_functions = {
    for k, v in merge(local.default_functions, var.functions) : k => {
      name                = k
      memory_size         = coalesce(lookup(v, "memory_size", null), var.lambda_memory_size)
      timeout             = coalesce(lookup(v, "timeout", null), var.lambda_timeout)
      http_method         = v.http_method
      resource_path       = v.resource_path
      source_arn          = lookup(v, "source_arn", "${var.apigw_execution_arn}/*/${v.http_method}${v.resource_path}")
      statement_id_prefix = coalesce(lookup(v, "statement_id_prefix", null), "AllowAPIGatewayInvoke")
      principal           = coalesce(lookup(v, "principal", null), "apigateway.amazonaws.com")
      env_variables       = coalesce(lookup(v, "env_variables", null), {})
      source_arn          = coalesce(lookup(v, "source_arn", null), "${var.apigw_execution_arn}/*/${v.http_method}${v.resource_path}")
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

  statement_id  = "${each.value.statement_id_prefix}-${each.key}"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.lambda[each.key].arn
  principal     = each.value.principal
  source_arn    = each.value.source_arn
}
