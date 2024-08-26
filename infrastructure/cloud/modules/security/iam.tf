# ECS
resource "aws_iam_role" "ecs_execution_role" {
  name = "${var.app_name}-ecs-execution-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_role_policy" "ecs_execution_policy" {
  name = "${var.app_name}-ecs-execution-policy-${var.environment}"
  role = aws_iam_role.ecs_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Action = [
          "ecr:GetAuthorizationToken",
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:GetRepositoryPolicy",
          "ecr:DescribeRepositories",
          "ecr:ListImages",
          "ecr:DescribeImages",
          "ecr:BatchGetImage",
          "ecr:GetLifecyclePolicy",
          "ecr:GetLifecyclePolicyPreview",
          "ecr:ListTagsForResource",
          "ecr:DescribeImageScanFindings"
        ],
        Effect = "Allow",
        Resource = [
          var.ecs_web_td_arn
        ]
      },
      {
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:CreateLogGroup"
        ],
        Effect = "Allow",
        Resource = [
          var.ecs_web_td_arn
        ]
      }
    ]
  })
}

# KMS
resource "aws_kms_key_policy" "kms_key_policy" {
  key_id = aws_kms_key.kms_key.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "kms:Decrypt",
          "kms:Encrypt",
          "kms:GenerateDataKey"
        ]
        Resource = "*"
      }
    ]
  })
}
