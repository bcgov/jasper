
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