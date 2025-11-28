resource "aws_ssm_parameter" "api_image_param" {
  name      = "/images/${var.app_name}-api-image-param-${var.environment}"
  type      = "String"
  value     = "dummy"
  overwrite = true

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "web_image_param" {
  name      = "/images/${var.app_name}-web-image-param-${var.environment}"
  type      = "String"
  value     = "dummy"
  overwrite = true

  lifecycle {
    ignore_changes = [value]
  }
}
