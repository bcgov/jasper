resource "aws_lb" "lb" {
  name               = "${var.app_name}-lb-${var.environment}"
  subnets            = local.web_subnets
  security_groups    = [aws_security_group.sg.id]
  internal           = true
  load_balancer_type = "application"
  enable_http2       = true

  tags = {
    Name = "${var.app_name}-lb-${var.environment}"
  }
}

resource "aws_lb_target_group" "lb_target_group" {
  name                 = "${var.app_name}-lb-tg-${var.environment}"
  port                 = 8080
  protocol             = "HTTP"
  vpc_id               = data.aws_vpc.vpc.id
  target_type          = "ip"
  deregistration_delay = 5

  health_check {
    enabled             = true
    interval            = 15
    path                = "/"
    port                = 8080
    protocol            = "HTTP"
    timeout             = 10
    healthy_threshold   = 2
    unhealthy_threshold = 3
    matcher             = "200"
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_lb_listener" "lb_listener" {
  load_balancer_arn = aws_lb.lb.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lb_target_group.arn
  }
}
