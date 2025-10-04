output "sns_topic_arn" {
  description = "The ARN of the SNS topic"
  value       = aws_sns_topic.topic.arn
}

output "sns_topic_name" {
  description = "The name of the SNS topic"
  value       = aws_sns_topic.topic.name
}

output "email_subscription_arns" {
  description = "The ARNs of the email subscriptions"
  value       = aws_sns_topic_subscription.email_subs[*].arn
}
