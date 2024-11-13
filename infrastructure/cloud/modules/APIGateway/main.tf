resource "aws_api_gateway_rest_api" "apigw" {
  name = "${var.app_name}-api-gateway-${var.environment}"

}

resource "aws_api_gateway_deployment" "apigw_deployment" {
  depends_on = [
    aws_api_gateway_integration.get_locations_integration,
    aws_api_gateway_integration.get_locations_rooms_integration,
    aws_api_gateway_integration.get_files_civil_integration,
    aws_api_gateway_integration.get_files_criminal_integration,
  ]
  rest_api_id = aws_api_gateway_rest_api.apigw.id
  stage_name  = var.environment
}

resource "aws_api_gateway_rest_api_policy" "apigw_rest_api_policy" {
  rest_api_id = aws_api_gateway_rest_api.apigw.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = var.ecs_execution_role_arn
        Action    = "execute-api:Invoke"
        Resource  = "arn:aws:execute-api:${var.region}:${var.account_id}:${aws_api_gateway_rest_api.apigw.id}/*"
      }
    ]
  })
}

resource "aws_api_gateway_usage_plan" "apigw_usage_plan" {
  name = "${var.app_name}-apigw-usage-plan-${var.environment}"

  api_stages {
    api_id = aws_api_gateway_rest_api.apigw.id
    stage  = aws_api_gateway_deployment.apigw_deployment.stage_name
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
  authorization    = "AWS_IAM"
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
  authorization    = "AWS_IAM"
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
  authorization    = "AWS_IAM"
  api_key_required = true
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
  authorization    = "AWS_IAM"
  api_key_required = true
}

resource "aws_api_gateway_integration" "get_files_criminal_integration" {
  rest_api_id             = aws_api_gateway_rest_api.apigw.id
  resource_id             = aws_api_gateway_resource.criminal_resource.id
  http_method             = aws_api_gateway_method.get_files_criminal_method.http_method
  type                    = "AWS_PROXY"
  integration_http_method = "POST"
  uri                     = var.lambda_functions["search-criminal-files"].invoke_arn
}
