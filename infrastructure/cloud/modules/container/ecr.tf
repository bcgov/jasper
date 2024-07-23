resource "aws_ecr_repository" "aws_ecr_repository" {
  name                 = "${var.ecr_repository_name}-${var.environment}"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    env  = var.environment
    name = "${var.ecr_repository_name}-${var.environment}"
  }
}
