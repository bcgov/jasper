output "alb_logs_bucket_name" {
  description = "The name of the ALB logs S3 bucket"
  value       = aws_s3_bucket.alb_logs_bucket.bucket
}
