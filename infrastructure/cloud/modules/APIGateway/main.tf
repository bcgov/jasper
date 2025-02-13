resource "aws_api_gateway_rest_api" "apigw" {
  name = "${var.app_name}-api-gateway-${var.environment}"

  binary_media_types = ["application/octet-stream"]
}

resource "aws_api_gateway_deployment" "apigw_deployment" {
  depends_on = [
    aws_api_gateway_integration.lambda_integration,
  ]
  rest_api_id = aws_api_gateway_rest_api.apigw.id

  triggers = {
    redeployment = sha1(jsonencode({
      binary_media_types = aws_api_gateway_rest_api.apigw.binary_media_types
      body               = aws_api_gateway_rest_api.apigw.body
    }))
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

# Root Resource /
resource "aws_api_gateway_resource" "root_resource" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  parent_id   = aws_api_gateway_rest_api.apigw.root_resource_id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "root_method" {
  rest_api_id      = aws_api_gateway_rest_api.apigw.id
  resource_id      = aws_api_gateway_resource.root_resource.id
  http_method      = "ANY"
  authorization    = "CUSTOM"
  authorizer_id    = aws_api_gateway_authorizer.authorizer.id
  api_key_required = true

  request_parameters = {
    "method.request.header.x-origin-verify" = true
  }
}

resource "aws_api_gateway_integration" "lambda_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.root_resource.id
  http_method             = aws_api_gateway_method.root_method.http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = var.lambda_functions["proxy-request"].invoke_arn
}
