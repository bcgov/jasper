resource "aws_ecr_repository" "ecr_repository" {
  name                 = "${var.app_name}-repo-${var.environment}"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    name = "${var.app_name}-repo-${var.environment}"
  }
}
