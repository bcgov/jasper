output "default_lb_dns_name" {
  value = data.aws_lb.default_lb.dns_name
}

output "default_lb_arn" {
  value = data.aws_lb.default_lb.arn
}
