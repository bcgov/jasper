resource "aws_ecr_repository" "ecr_repository" {
  name                 = "${var.app_name}-ecr-repo-${var.environment}"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    name = "${var.app_name}-ecr-repo-${var.environment}"
  }
}
