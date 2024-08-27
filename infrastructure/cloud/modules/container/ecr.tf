resource "aws_ecr_repository" "ecr_repository" {
  name                 = "${var.app_name}-ecr-repo-${var.environment}"
  force_delete         = true
  image_tag_mutability = "IMMUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "KMS"
    kms_key         = var.kms_key_id
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = {
    name = "${var.app_name}-ecr-repo-${var.environment}"
  }
}
