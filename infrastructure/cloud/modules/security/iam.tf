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
          var.ecr_repository_arn
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
          "${var.ecs_web_td_log_group_arn}:*",
          "${var.ecs_api_td_log_group_arn}:*"
        ]
      },
      {
        Action = [
          "secretsmanager:GetSecretValue"
        ],
        Effect = "Allow",
        Resource = [
          aws_secretsmanager_secret.aspnet_core_secret.arn,
          aws_secretsmanager_secret.file_services_client_secret.arn,
          aws_secretsmanager_secret.location_services_client_secret.arn,
          aws_secretsmanager_secret.lookup_services_client_secret.arn,
          aws_secretsmanager_secret.user_services_client_secret.arn,
          aws_secretsmanager_secret.keycloak_secret.arn,
          aws_secretsmanager_secret.request_secret.arn,
          aws_secretsmanager_secret.splunk_secret.arn,
          aws_secretsmanager_secret.database_secret.arn,
          aws_secretsmanager_secret.aspnet_core_secret.arn,
          aws_secretsmanager_secret.misc_secret.arn,
          aws_secretsmanager_secret.auth_secret.arn
        ]
      },
      {
        Action = [
          "kms:Decrypt"
        ],
        Effect   = "Allow",
        Resource = aws_kms_key.kms_key.arn
      }
    ]
  })
}

#
# RolesAnywhere
#
# resource "aws_iam_role" "rolesanywhere_role" {
#   name = "${var.app_name}-rolesanywhere-role-${var.environment}"

#   assume_role_policy = jsonencode({
#     Version = "2012-10-17"
#     Statement = [{
#       Action = [
#         "sts:AssumeRole",
#         "sts:TagSession",
#         "sts:SetSourceIdentity"
#       ]
#       Principal = {
#         Service = "rolesanywhere.amazonaws.com"
#       }
#       Effect = "Allow"
#     }]
#   })
# }

# Openshift
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
          "kms:Decrypt"
        ],
        "Effect" : "Allow",
        "Resource" : [
          "arn:aws:ssm:*:*:parameter/iam_users/*",
          "arn:aws:kms:*:*:key/*"
        ]
      },
      {
        Action = [
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret",
          "secretsmanager:PutSecretValue",
        ]
        Effect = "Allow"
        Resource = [
          aws_secretsmanager_secret.aspnet_core_secret.arn,
          aws_secretsmanager_secret.auth_secret.arn,
          aws_secretsmanager_secret.database_secret.arn,
          aws_secretsmanager_secret.file_services_client_secret.arn,
          aws_secretsmanager_secret.keycloak_secret.arn,
          aws_secretsmanager_secret.location_services_client_secret.arn,
          aws_secretsmanager_secret.lookup_services_client_secret.arn,
          aws_secretsmanager_secret.misc_secret.arn,
          aws_secretsmanager_secret.request_secret.arn,
          aws_secretsmanager_secret.splunk_secret.arn,
          aws_secretsmanager_secret.user_services_client_secret.arn
        ]
      }
    ]
  })
}

data "aws_iam_user" "openshift_user" {
  user_name = var.openshift_iam_user
}

resource "aws_iam_role_policy_attachment" "my_role_policy_attachment" {
  role       = aws_iam_role.openshift_role.name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
}

resource "aws_iam_user_policy_attachment" "openshift_user_policy_attachment" {
  user       = data.aws_iam_user.openshift_user.user_name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
}
