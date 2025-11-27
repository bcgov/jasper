# S3 bucket for ALB access logs
resource "aws_s3_bucket" "alb_logs_bucket" {
  bucket = "${var.app_name}-alb-logs-${var.environment}"

  tags = {
    Application = "${var.app_name}-${var.environment}"
    Name        = "${var.app_name}-alb-logs-${var.environment}"
    Environment = var.environment
  }
}

# Block public access to the ALB logs bucket
resource "aws_s3_bucket_public_access_block" "alb_logs_bucket_public_access_block" {
  bucket = aws_s3_bucket.alb_logs_bucket.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Enable server-side encryption for ALB logs bucket
resource "aws_s3_bucket_server_side_encryption_configuration" "alb_logs_bucket_encryption" {
  bucket = aws_s3_bucket.alb_logs_bucket.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# Bucket policy to allow ALB to write logs
# Reference: https://docs.aws.amazon.com/elasticloadbalancing/latest/application/enable-access-logging.html
resource "aws_s3_bucket_policy" "alb_logs_bucket_policy" {
  bucket = aws_s3_bucket.alb_logs_bucket.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AWSLogDeliveryWrite"
        Effect = "Allow"
        Principal = {
          Service = "elasticloadbalancing.amazonaws.com"
        }
        Action   = "s3:PutObject"
        Resource = "${aws_s3_bucket.alb_logs_bucket.arn}/*"
      },
      {
        Sid    = "AWSLogDeliveryAclCheck"
        Effect = "Allow"
        Principal = {
          Service = "elasticloadbalancing.amazonaws.com"
        }
        Action   = "s3:GetBucketAcl"
        Resource = aws_s3_bucket.alb_logs_bucket.arn
      }
    ]
  })
}

# Lifecycle policy to manage old logs
resource "aws_s3_bucket_lifecycle_configuration" "alb_logs_bucket_lifecycle" {
  bucket = aws_s3_bucket.alb_logs_bucket.id

  rule {
    id     = "expire-old-logs"
    status = "Enabled"

    expiration {
      days = 90
    }

    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }
}
