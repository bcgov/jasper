resource "aws_ecr_repository" "ecr_repository" {
  name                 = "${var.app_name}-${var.repo_name}-repo-${var.environment}"
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
    ignore_changes = [
      encryption_configuration,
    ]
  }

  tags = {
    name = "${var.app_name}-${var.repo_name}-repo-${var.environment}"
  }
}

resource "aws_ecr_lifecycle_policy" "ecr_lifecycle_policy" {
  repository = aws_ecr_repository.ecr_repository.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Remove untagged images after 7 days"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 7
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 2
        description  = "Remove tagged images older than 30 days"
        selection = {
          tagStatus   = "tagged"
          tagPrefixList = ["api", "web"]
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 30
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
