resource "aws_lb_target_group" "target_group" {
  name                 = "${var.app_name}-${var.name}-tg-${var.environment}"
  port                 = var.port
  protocol             = var.protocol
  vpc_id               = var.vpc_id
  target_type          = "ip"
  deregistration_delay = 5

  health_check {
    protocol            = var.protocol
    path                = var.health_check_path
    port                = var.port
    interval            = 30
    timeout             = 5
    enabled             = true
    healthy_threshold   = 3
    unhealthy_threshold = 3
    matcher             = "200"
  }
}
