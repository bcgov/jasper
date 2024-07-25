output "subnet_private_id" {
  description = "Private Subnet ID"
  value       = aws_subnet.private[*].id
}

output "ecs_sg_id" {
  description = "ECS Security Group ID"
  value       = aws_security_group.ecs_security_group.id
}

output "lb_tg_arn" {
  description = "Load Balancer Target Group ARN"
  value       = aws_lb_target_group.lb_target_group.arn
}

output "lb_listener" {
  description = "Load Balancer Listener"
  value       = aws_lb_listener.lb_listener
}
