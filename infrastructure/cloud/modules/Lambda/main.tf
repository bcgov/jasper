locals {
  functions = {
    "authorizer" = {
      http_method       = "*"
      resource_path     = ""
      enable_vpc_config = false
      env_variables = {
        VERIFY_SECRET_NAME = var.lambda_secrets["authorizer"]
      }
    }
    "rotate-key" = {
      http_method         = "POST"
      resource_path       = "/*"
      statement_id_prefix = "AllowSecretsManagerInvoke"
      source_arn          = var.lambda_secrets["authorizer_arn"]
      principal           = "secretsmanager.amazonaws.com"
      enable_vpc_config   = false
      env_variables = {
        VERIFY_SECRET_NAME = var.lambda_secrets["authorizer"]
        CLUSTER_NAME       = var.ecs_cluster_name
      }
    }
    "proxy-request" = {
      http_method       = "*"
      resource_path     = ""
      enable_vpc_config = true
      enable_efs        = true
      env_variables = {
        FILE_SERVICES_CLIENT_SECRET_NAME = var.lambda_secrets["file_services_client"]
        PCSS_SECRET_NAME                 = var.lambda_secrets["pcss"]
        DARS_SECRET_NAME                 = var.lambda_secrets["dars"]
        EFS_MOUNT_PATH                   = var.efs_mount_path
      }
    }
  }

  lambda_functions = {
    for k, v in local.functions : k => {
      name                = k
      memory_size         = coalesce(lookup(v, "memory_size", null), var.lambda_memory_size)
      timeout             = coalesce(lookup(v, "timeout", null), var.lambda_timeout)
      http_method         = v.http_method
      resource_path       = v.resource_path
      source_arn          = lookup(v, "source_arn", "${var.apigw_execution_arn}/*/${v.http_method}${v.resource_path}")
      statement_id_prefix = coalesce(lookup(v, "statement_id_prefix", null), "AllowAPIGatewayInvoke")
      principal           = coalesce(lookup(v, "principal", null), "apigateway.amazonaws.com")
      env_variables       = v.env_variables
      source_arn          = coalesce(lookup(v, "source_arn", null), "${var.apigw_execution_arn}/*/${v.http_method}${v.resource_path}")
      enable_vpc_config   = coalesce(lookup(v, "enable_vpc_config", null), true)
      enable_efs          = coalesce(lookup(v, "enable_efs", null), false)
    }
  }

  default_env_variables = {
    MTLS_SECRET_NAME = var.lambda_secrets["mtls"]
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

  dynamic "vpc_config" {
    for_each = each.value.enable_vpc_config ? [1] : []
    content {
      subnet_ids         = var.subnet_ids
      security_group_ids = var.sg_ids
    }
  }

  dynamic "file_system_config" {
    for_each = each.value.enable_efs && var.efs_access_point_arn != "" ? [1] : []
    content {
      arn              = var.efs_access_point_arn
      local_mount_path = var.efs_mount_path
    }
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
