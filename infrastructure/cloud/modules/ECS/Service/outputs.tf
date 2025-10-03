output "ecs_service_arn" {
  value = aws_ecs_service.ecs_service.id
}

output "service_name" {
  description = "The name of the ECS service"
  value       = aws_ecs_service.ecs_service.name
}

output "scale_up_policy_arn" {
  description = "ARN of the scale up policy"
  value       = aws_appautoscaling_policy.ecs_policy_up.arn
}

output "scale_down_policy_arn" {
  description = "ARN of the scale down policy"
  value       = aws_appautoscaling_policy.ecs_policy_down.arn
}
