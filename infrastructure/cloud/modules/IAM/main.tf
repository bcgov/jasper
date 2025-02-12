#
# KMS Key Policy
#
resource "aws_kms_key_policy" "kms_key_policy" {
  key_id = var.kms_key_id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      # Allow full access to the key for administrators
      {
        Sid    = "EnableIAMUserPermissions"
        Effect = "Allow"
        Principal = {
          AWS = [
            "arn:aws:iam::${var.account_id}:root",
            "arn:aws:iam::${var.account_id}:user/${var.openshift_iam_user}"
          ]
        }
        Action   = "kms:*"
        Resource = "*"
      },

      # Allow CloudWatch Logs to use the key
      {
        Sid    = "AllowCloudWatchLogsUsage"
        Effect = "Allow"
        Principal = {
          Service = "logs.amazonaws.com"
        }
        Action   = "kms:*"
        Resource = "*"
      },

      # Allow Lambda Functions to use the key
      {
        Sid    = "AllowLambdasUsage"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Action   = "kms:*"
        Resource = "*"
      }
    ]
  })
}


#
# ECS
#
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
        Effect = "Allow",
        Action = [
          "ecr:GetAuthorizationToken"
        ],
        Resource = "*"
      },
      {
        Action = [
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
          var.app_ecr_repo_arn
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
          "arn:aws:logs:${var.region}:${var.account_id}:log-group:/aws/ecs/*:*"
        ]
      },
      {
        Action = [
          "secretsmanager:GetSecretValue"
        ],
        Effect   = "Allow",
        Resource = var.secrets_arn_list
      },
      {
        Action = [
          "kms:Decrypt"
        ],
        Effect   = "Allow",
        Resource = var.kms_key_arn
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "rds:DescribeDBInstances",
          "rds:Connect",
          "rds:DescribeDBSecurityGroups"
        ],
        "Resource" : "arn:aws:rds:${var.region}:${var.account_id}:db:${var.app_name}-postgres-db-${var.environment}"
      }
    ]
  })
}

# Attach the AmazonECSTaskExecutionRolePolicy
resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# Attach the AmazonSSMManagedInstanceCore policy for ECS Exec
resource "aws_iam_role_policy_attachment" "ecs_task_ssm_policy" {
  role       = aws_iam_role.ecs_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# (Optional) Attach CloudWatch Logs policy if logging is needed
resource "aws_iam_role_policy_attachment" "ecs_task_cloudwatch_policy" {
  role       = aws_iam_role.ecs_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchLogsFullAccess"
}

data "aws_iam_user" "openshift_user" {
  user_name = var.openshift_iam_user
}

resource "aws_iam_user_policy_attachment" "openshift_user_policy_attachment" {
  user       = data.aws_iam_user.openshift_user.user_name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
}

resource "aws_iam_role" "openshift_role" {
  name = "${var.app_name}-openshift-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "secretsmanager.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_policy" "openshift_role_policy" {
  name        = "${var.app_name}-openshift-role-policy-${var.environment}"
  description = "Policy to allow access to specific secrets in Secrets Manager for Openshift"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        "Action" : [
          "ssm:GetParameter",
          "ssm:GetParameters",
          "ssm:GetParametersByPath",
          "kms:Encrypt",
          "kms:Decrypt"
        ],
        "Effect" : "Allow",
        "Resource" : [
          "arn:aws:ssm:${var.region}:${var.account_id}:parameter/iam_users/${var.openshift_iam_user}_keys",
          var.kms_key_arn
        ]
      },
      {
        Action = [
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret",
          "secretsmanager:PutSecretValue",
        ]
        Effect   = "Allow"
        Resource = var.secrets_arn_list
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "openshift_role_policy_attachment" {
  role       = aws_iam_role.openshift_role.name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
}

#
# Lambda
#
resource "aws_iam_role" "lambda_role" {
  name = "${var.app_name}-lambda-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_policy" "lambda_role_policy" {
  name = "${var.app_name}-lambda-role-policy-${var.environment}"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        "Action" : [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:GenerateDataKey",
          "kms:GenerateDataKeyWithoutPlaintext"
        ],
        "Effect" : "Allow",
        "Resource" : [
          var.kms_key_arn
        ]
      },
      {
        Action = [
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret",
          "secretsmanager:PutSecretValue",
          "secretsmanager:UpdateSecret",
          "secretsmanager:UpdateSecretVersionStage"
        ]
        Effect   = "Allow"
        Resource = var.secrets_arn_list
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.region}:${var.account_id}:log-group:/aws/lambda/${var.app_name}-*-lambda-${var.environment}:*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ecs:UpdateService",
          "ecs:DescribeServices",
          "ecs:ListServices"
        ],
        "Resource" : [
          "arn:aws:ecs:${var.region}:${var.account_id}:service/${var.app_name}-app-cluster-${var.environment}/*"
        ]
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ecr:GetAuthorizationToken",
          "ecr:BatchGetImage",
          "ecr:BatchCheckLayerAvailability"
        ],
        "Resource" : [
          "arn:aws:ecr:${var.region}:${var.account_id}:repository/${var.app_name}-*-repo-${var.environment}"
        ]
      },
      {
        "Action" : [
          "ec2:CreateNetworkInterface",
          "ec2:DescribeNetworkInterfaces",
          "ec2:DeleteNetworkInterface",
          "ec2:AttachNetworkInterface",
          "ec2:DetachNetworkInterface"
        ],
        "Effect" : "Allow",
        "Resource" : "arn:aws:ec2:${var.region}:*:network-interface/*",
        "Condition" : {
          "ArnLikeIfExists" : {
            "ec2:Vpc" : "arn:aws:ec2:${var.region}:*:vpc/${var.vpc_id}"
          }
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_role_policy_attachment" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = aws_iam_policy.lambda_role_policy.arn
}

#
# API Gateway
#
resource "aws_iam_role" "apigw_logging_role" {
  name = "${var.app_name}-apigw-logging-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "apigateway.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

# Attach the AmazonAPIGatewayPushToCloudWatchLogs policy
resource "aws_iam_role_policy_attachment" "apigw_logging_role_policy" {
  role       = aws_iam_role.apigw_logging_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonAPIGatewayPushToCloudWatchLogs"
}
