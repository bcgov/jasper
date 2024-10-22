data "aws_caller_identity" "current" {}

# kms key for encryption
resource "aws_kms_key" "kms_key" {
  description             = "KMS key for encryption"
  deletion_window_in_days = 10
  enable_key_rotation     = true
  is_enabled              = true
  #   policy                  = data.aws_iam_policy_document.kms_policy.json
  tags = {
    Application = "${var.app_name}-${var.environment}"
    Name        = "${var.kms_key_name}-${var.environment}"
    Environment = "${var.environment}"
  }
}

resource "aws_kms_alias" "kms_alias" {
  name          = "alias/${var.kms_key_name}-${var.environment}"
  target_key_id = aws_kms_key.kms_key.key_id
}

resource "aws_kms_key_policy" "kms_key_policy" {
  key_id = aws_kms_key.kms_key.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      # Allow full access to the key for administrators
      {
        Sid    = "EnableIAMUserPermissions"
        Effect = "Allow"
        Principal = {
          AWS = [
            "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root",
            "arn:aws:iam::${data.aws_caller_identity.current.account_id}:user/${var.openshift_iam_user}",
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
