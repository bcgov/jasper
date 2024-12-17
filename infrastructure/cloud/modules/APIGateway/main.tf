resource "aws_api_gateway_rest_api" "apigw" {
  name = "${var.app_name}-api-gateway-${var.environment}"

}

resource "aws_api_gateway_deployment" "apigw_deployment" {
  depends_on = [
    # Add new integration here so that it registers in API Gateway
    aws_api_gateway_integration.get_locations_integration,
    aws_api_gateway_integration.get_locations_rooms_integration,
    aws_api_gateway_integration.get_files_civil_integration,
    aws_api_gateway_integration.get_files_criminal_integration,
  ]
  rest_api_id = aws_api_gateway_rest_api.apigw.id

  triggers = {
    redeployment = sha1(jsonencode(aws_api_gateway_rest_api.apigw.body))
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_api_gateway_stage" "apigw_stage" {
  stage_name           = var.environment
  deployment_id        = aws_api_gateway_deployment.apigw_deployment.id
  rest_api_id          = aws_api_gateway_rest_api.apigw.id
  xray_tracing_enabled = true

  access_log_settings {
    destination_arn = var.log_group_arn
    format = jsonencode({
      requestId      = "$context.requestId",
      ip             = "$context.identity.sourceIp",
      caller         = "$context.identity.caller",
      user           = "$context.identity.user",
      requestTime    = "$context.requestTime",
      httpMethod     = "$context.httpMethod",
      resourcePath   = "$context.resourcePath",
      status         = "$context.status",
      protocol       = "$context.protocol",
      responseLength = "$context.responseLength"
    })
  }
}

resource "aws_api_gateway_method_settings" "apgw_method_settings" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  stage_name  = aws_api_gateway_stage.apigw_stage.stage_name
  method_path = "*/*"

  settings {
    data_trace_enabled   = true
    metrics_enabled      = true
    logging_level        = "INFO"
    cache_data_encrypted = true
    caching_enabled      = true
  }
}

resource "aws_api_gateway_rest_api_policy" "apigw_rest_api_policy" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          AWS = var.ecs_execution_role_arn
        }
        Action   = "execute-api:Invoke"
        Resource = "arn:aws:execute-api:${var.region}:${var.account_id}:${aws_api_gateway_rest_api.apigw.id}/*"
      }
    ]
  })
}

resource "aws_api_gateway_account" "apigateway_account" {
  cloudwatch_role_arn = var.apigw_logging_role_arn
}

resource "aws_api_gateway_usage_plan" "apigw_usage_plan" {
  name = "${var.app_name}-apigw-usage-plan-${var.environment}"

  api_stages {
    api_id = aws_api_gateway_rest_api.apigw.id
    stage  = aws_api_gateway_stage.apigw_stage.stage_name
  }
}

resource "aws_api_gateway_api_key" "apigw_api_key" {
  name = "${var.app_name}-apigw-api-key-${var.environment}"
}

resource "aws_api_gateway_usage_plan_key" "apigw_usage_plan_key" {
  key_id        = aws_api_gateway_api_key.apigw_api_key.id
  key_type      = "API_KEY"
  usage_plan_id = aws_api_gateway_usage_plan.apigw_usage_plan.id
}

#
# Authorizer
#
resource "aws_api_gateway_authorizer" "authorizer" {
  name            = "${var.app_name}-authorizer-${var.environment}"
  rest_api_id     = aws_api_gateway_rest_api.apigw.id
  authorizer_uri  = var.lambda_functions["authorizer"].invoke_arn
  type            = "REQUEST"
  identity_source = "method.request.header.x-origin-verify"
}

#
# /locations Resource
#
resource "aws_api_gateway_resource" "locations_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_rest_api.apigw.root_resource_id
  path_part   = "locations"
}

# GET /locations
resource "aws_api_gateway_method" "get_locations_method" {
  rest_api_id      = aws_api_gateway_rest_api.apigw.id
  resource_id      = aws_api_gateway_resource.locations_resource.id
  http_method      = var.lambda_functions["get-locations"].http_method
  authorization    = "CUSTOM"
  authorizer_id    = aws_api_gateway_authorizer.authorizer.id
  api_key_required = true

  request_parameters = {
    "method.request.header.x-origin-verify" = true
  }
}

resource "aws_api_gateway_integration" "get_locations_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.locations_resource.id
  http_method             = aws_api_gateway_method.get_locations_method.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST"
  uri                     = var.lambda_functions["get-locations"].invoke_arn
}

# /locations/rooms Resource
resource "aws_api_gateway_resource" "rooms_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_resource.locations_resource.id
  path_part   = "rooms"
}

# GET /locations/rooms
resource "aws_api_gateway_method" "get_locations_rooms_method" {
  rest_api_id      = aws_api_gateway_rest_api.apigw.id
  resource_id      = aws_api_gateway_resource.rooms_resource.id
  http_method      = var.lambda_functions["get-rooms"].http_method
  authorization    = "CUSTOM"
  authorizer_id    = aws_api_gateway_authorizer.authorizer.id
  api_key_required = true

  request_parameters = {
    "method.request.header.x-origin-verify" = true
  }
}

resource "aws_api_gateway_integration" "get_locations_rooms_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.rooms_resource.id
  http_method             = aws_api_gateway_method.get_locations_rooms_method.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST"
  uri                     = var.lambda_functions["get-rooms"].invoke_arn
}

#
# /files Resource
#
resource "aws_api_gateway_resource" "files_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_rest_api.apigw.root_resource_id
  path_part   = "files"
}

# /files/civil Resource
resource "aws_api_gateway_resource" "civil_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_resource.files_resource.id
  path_part   = "civil"
}

# GET /files/civil
resource "aws_api_gateway_method" "get_files_civil_method" {
  rest_api_id      = aws_api_gateway_rest_api.apigw.id
  resource_id      = aws_api_gateway_resource.civil_resource.id
  http_method      = var.lambda_functions["search-civil-files"].http_method
  authorization    = "CUSTOM"
  authorizer_id    = aws_api_gateway_authorizer.authorizer.id
  api_key_required = true

  request_parameters = {
    "method.request.header.x-origin-verify" = true
  }
}

resource "aws_api_gateway_integration" "get_files_civil_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.civil_resource.id
  http_method             = aws_api_gateway_method.get_files_civil_method.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST"
  uri                     = var.lambda_functions["search-civil-files"].invoke_arn
}

# /files/criminal Resource
resource "aws_api_gateway_resource" "criminal_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_resource.files_resource.id
  path_part   = "criminal"
}

# GET /files/criminal
resource "aws_api_gateway_method" "get_files_criminal_method" {
  rest_api_id      = aws_api_gateway_rest_api.apigw.id
  resource_id      = aws_api_gateway_resource.criminal_resource.id
  http_method      = var.lambda_functions["search-criminal-files"].http_method
  authorization    = "CUSTOM"
  authorizer_id    = aws_api_gateway_authorizer.authorizer.id
  api_key_required = true

  request_parameters = {
    "method.request.header.x-origin-verify" = true
  }
}

resource "aws_api_gateway_integration" "get_files_criminal_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.criminal_resource.id
  http_method             = aws_api_gateway_method.get_files_criminal_method.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST"
  uri                     = var.lambda_functions["search-criminal-files"].invoke_arn
}
