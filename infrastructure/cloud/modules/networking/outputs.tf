output "sg_id" {
  value = aws_security_group.sg.id
}

output "lb_tg_arn" {
  value = aws_lb_target_group.lb_target_group.arn
}

output "ecs_sg_id" {
  value = aws_security_group.ecs_sg.id
}

output "web_subnets_ids" {
  value = local.web_subnets
}
