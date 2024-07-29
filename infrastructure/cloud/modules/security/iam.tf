resource "aws_iam_role" "ecs_web_task_execution_role" {
  name = "${var.app_name}-ecs-web-task-execution-role-${var.environment}"

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

resource "aws_iam_role_policy" "ecs_web_task_execution_policy" {
  name = "${var.app_name}-ecs-web-task-execution-policy-${var.environment}"
  role = aws_iam_role.ecs_web_task_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:BatchGetImage",
          "ecr:GetDownloadUrlForLayer",
          "ecr:GetAuthorizationToken"
        ]
        Resource = var.ecs_web_task_definition_arn
      }
    ]
  })
}
