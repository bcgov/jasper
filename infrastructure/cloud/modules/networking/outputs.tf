output "sg_id" {
  value = aws_security_group.sg.id
}

output "lb_tg_arn" {
  value = aws_lb_target_group.lb_target_group.arn
}

output "public_subnets" {
  value = [aws_subnet.public_subnets[0].id, aws_subnet.public_subnets[1].id]

}
output "private_subnets_web" {
  value = [aws_subnet.private_subnets_web[0].id, aws_subnet.private_subnets_web[1].id]
}
