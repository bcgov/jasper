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

resource "aws_iam_role_policy_attachment" "openshift_role_policy_attachment" {
  role       = aws_iam_role.openshift_role.name
  policy_arn = aws_iam_policy.openshift_role_policy.arn
}
