output "subnet_1_id" {
  value = aws_subnet.subnet_1.id
}

output "subnet_2_id" {
  value = aws_subnet.subnet_2.id
}

output "lb_sg_id" {
  value = aws_security_group.lb_sg.id
}

output "ecs_sg_id" {
  value = aws_security_group.ecs_sg.id
}

output "lb_tg_arn" {
  value = aws_lb_target_group.lb_target_group.arn
}
