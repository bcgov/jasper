resource "aws_s3_bucket" "certificates_s3_bucket" {
  bucket = "${var.app_name}-certificates-${var.environment}"
  tags = {
    Name        = "${var.app_name}-certificates-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "secure_bucket_encryption" {
  bucket = aws_s3_bucket.certificates_s3_bucket.id

  rule {
    apply_server_side_encryption_by_default {
      kms_master_key_id = var.kms_key_arn
      sse_algorithm     = "aws:kms"
    }
  }
}
