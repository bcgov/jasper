# SNS Topic
resource "aws_sns_topic" "topic" {
  name              = "${var.app_name}-${var.name}-${var.environment}"
  kms_master_key_id = var.kms_key_id

  tags = {
    Name        = "${var.app_name}-${var.name}-${var.environment}"
    Environment = var.environment
    Application = var.app_name
    Purpose     = var.purpose
  }
}

# Email Subscriptions
resource "aws_sns_topic_subscription" "email_subs" {
  count     = length(var.email_addresses)
  topic_arn = aws_sns_topic.topic.arn
  protocol  = "email"
  endpoint  = var.email_addresses[count.index]
}
