
data "aws_caller_identity" "current" {}

data "aws_kms_alias" "encryption_key_alias" {
  name = var.kms_key_name
}

# a test s3 bucket
resource "aws_s3_bucket" "test_s3_bucket" {
  bucket = "${var.test_s3_bucket_name}-${var.environment}"

  tags = {
    Application = "${var.app_name}-${var.environment}"
    Name        = "${var.test_s3_bucket_name}-${var.environment}"
    Environment = "${var.environment}"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "test_bucket_encryption" {
  bucket = aws_s3_bucket.test_s3_bucket.id

  rule {
    apply_server_side_encryption_by_default {
      kms_master_key_id = data.aws_kms_alias.encryption_key_alias.target_key_id
      sse_algorithm     = "aws:kms"
    }
  }
}

resource "aws_s3_bucket_ownership_controls" "test_bucket_ownership_controls" {
  bucket = aws_s3_bucket.test_s3_bucket.id
  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_acl" "test_bucket_acl" {
  depends_on = [aws_s3_bucket_ownership_controls.test_bucket_ownership_controls]

  bucket = aws_s3_bucket.test_s3_bucket.id
  acl    = "private"
}
