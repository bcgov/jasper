output "subnet_id" {
  value = aws_subnet.subnet[*].id
}

output "ecs_sg_id" {
  value = aws_security_group.ecs_security_group.id
}

output "lb_tg_arn" {
  value = aws_lb_target_group.lb_target_group.arn
}

output "lb_listener" {
  value = aws_lb_listener.lb_listener
}
