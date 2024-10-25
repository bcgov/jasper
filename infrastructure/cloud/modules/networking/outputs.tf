output "web_tg_arn" {
  value = aws_lb_target_group.web_target_group.arn
}

output "api_tg_arn" {
  value = aws_lb_target_group.api_target_group.arn
}

output "web_sg_id" {
  value = data.aws_security_group.web_sg.id
}

output "app_sg_id" {
  value = data.aws_security_group.app_sg.id
}

output "web_subnets_ids" {
  value = local.web_subnets
}

output "app_subnets_ids" {
  value = local.app_subnets
}
output "data_subnets_ids" {
  value = local.data_subnets
}

output "default_lb_dns_name" {
  value = data.aws_lb.default_lb.dns_name
}

output "data_sg_id" {
  value = data.aws_security_group.data_sg.id
}
