resource "aws_ssm_parameter" "api_image_param" {
  name  = "/images/jasper-api-image-param-${var.environment}"
  type  = "String"    
  value = "dummy"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "web_image_param" {
  name  = "/images/jasper-web-image-param-${var.environment}"
  type  = "String"    
  value = "dummy"

  lifecycle {
    ignore_changes = [value]
  }
}