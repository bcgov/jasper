# Web
resource "aws_lb_target_group" "web_target_group" {
  name                 = "${var.app_name}-web-tg-${var.environment}"
  port                 = 8080
  protocol             = "HTTPS"
  vpc_id               = data.aws_vpc.vpc.id
  target_type          = "ip"
  deregistration_delay = 5

  health_check {
    protocol            = "HTTPS"
    path                = "/"
    port                = 8080
    interval            = 30
    timeout             = 5
    enabled             = true
    healthy_threshold   = 3
    unhealthy_threshold = 3
    matcher             = "200"
  }
}

# API
resource "aws_lb_target_group" "api_target_group" {
  name                 = "${var.app_name}-api-tg-${var.environment}"
  port                 = 5000
  protocol             = "HTTP"
  vpc_id               = data.aws_vpc.vpc.id
  target_type          = "ip"
  deregistration_delay = 5

  health_check {
    path                = "/api/test/headers"
    port                = 5000
    interval            = 30
    timeout             = 5
    enabled             = true
    healthy_threshold   = 3
    unhealthy_threshold = 3
    matcher             = "200"
  }
}
