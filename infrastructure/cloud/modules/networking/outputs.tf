output "sg_id" {
  value = aws_security_group.sg.id
}

output "lb_tg_arn" {
  value = aws_lb_target_group.lb_target_group.arn
}

output "subnet_ids" {
  value = data.aws_subnets.default_public.ids
}
