output "ecs_service_arn" {
  value = aws_ecs_service.ecs_service.id
}

output "service_name" {
  description = "The name of the ECS service"
  value       = aws_ecs_service.ecs_service.name
}

output "cpu_target_tracking_policy_arn" {
  description = "ARN of the CPU target tracking policy"
  value       = aws_appautoscaling_policy.ecs_target_tracking_cpu.arn
}

output "memory_target_tracking_policy_arn" {
  description = "ARN of the memory target tracking policy"
  value       = aws_appautoscaling_policy.ecs_target_tracking_memory.arn
}
