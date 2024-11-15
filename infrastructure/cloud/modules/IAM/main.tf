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
          "arn:aws:logs:*:*:log-group:/aws/ecs/*:*"
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
        "Resource" : "arn:aws:rds:*:*:db:${var.app_name}-postgres-db-${var.environment}"
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

#
# Openshift
#
# https://developer.gov.bc.ca/docs/default/component/public-cloud-techdocs/design-build-and-deploy-an-application/iam-user-service/
# Step 1: Add opeshiftuser if not exist
data "aws_dynamodb_table" "iam_user_table" {
  name = var.iam_user_table_name
}

resource "null_resource" "check_and_insert_openshiftuser_record" {
  triggers = {
    always_run = timestamp()
  }

  provisioner "local-exec" {
    command = <<EOT
      # Check if the 'openshiftuser' record exists
      EXISTS=$(aws dynamodb get-item --table-name ${var.iam_user_table_name} --key '{"UserName": {"S": "${var.openshift_iam_user}"}}' --query 'Item')

      # Check for errors from the previous command
      if [ $? -ne 0 ]; then
        echo "Failed to check for record: $EXISTS"
        exit 1
      fi

      # If the record does not exist, insert it
      if [ -z "$EXISTS" ] || [ "$EXISTS" = "null" ]; then
        echo "Record does not exist. Inserting..."
        aws dynamodb put-item --table-name ${var.iam_user_table_name} --item '{"UserName": {"S": "${var.openshift_iam_user}"}}'
      else
        echo "Record already exists. No action taken."
      fi
    EOT
  }
}

# Step 2: Wait for openshift user to become available
resource "null_resource" "wait_for_openshift_iam_user" {
  depends_on = [null_resource.check_and_insert_openshiftuser_record]
  provisioner "local-exec" {
    command = <<EOT
    for i in {1..10}; do
      aws iam get-user --user-name ${var.openshift_iam_user} && exit 0 || echo "Waiting for user..." && sleep 10
    done
    exit 1
    EOT
  }
}

data "aws_iam_user" "openshift_user" {
  user_name  = var.openshift_iam_user
  depends_on = [null_resource.wait_for_openshift_iam_user]
}

# Step 3: Attach policy to openshift user
resource "aws_iam_user_policy_attachment" "openshift_user_policy_attachment" {
  user       = data.aws_iam_user.openshift_user.user_name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
  depends_on = [null_resource.wait_for_openshift_iam_user]
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
          "arn:aws:ssm:*:*:parameter/iam_users/${var.openshift_iam_user}_keys",
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
        Resource = "arn:aws:logs:*:*:log-group:/aws/lambda/${var.app_name}-*-lambda-${var.environment}:*"
      },
      {
        "Effect" : "Allow",
        "Action" : [
          "ecs:UpdateService",
          "ecs:DescribeServices",
          "ecs:ListServices"
        ],
        "Resource" : [
          "arn:aws:ecs:*:*:cluster/${var.app_name}-app-cluster-${var.environment}",
          "arn:aws:ecs:*:*:service/${var.app_name}-app-cluster-${var.environment}/${var.app_name}-*-ecs-service-${var.environment}"
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
          "arn:aws:ecr:*:*:repository/${var.app_name}-*-repo-${var.environment}"
        ]
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
