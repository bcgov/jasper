resource "aws_lb" "lb" {
  name                       = "${var.app_name}-lb-${var.environment}"
  subnets                    = local.web_subnets
  security_groups            = [aws_security_group.lb_sg.id]
  internal                   = true
  load_balancer_type         = "application"
  enable_http2               = true
  drop_invalid_header_fields = true

  tags = {
    Name = "${var.app_name}-lb-${var.environment}"
  }
}

data "aws_lb" "default_lb" {
  name = var.lb_name
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

resource "aws_lb_listener" "http" {
  load_balancer_arn = data.aws_lb.default_lb.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"

    redirect {
      host        = "#{host}"
      path        = "/"
      port        = "443"
      protocol    = "HTTPS"
      query       = "#{query}"
      status_code = "HTTP_301" # Use HTTP_302 for temporary redirects
    }
  }
}

data "aws_lb_listener" "https_listener" {
  load_balancer_arn = data.aws_lb.default_lb.arn
  port              = 443
}

resource "aws_lb_listener_rule" "web_path_lr" {

  listener_arn = data.aws_lb_listener.https_listener.arn
  priority     = 1

  condition {
    path_pattern {
      values = ["/app"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lb_target_group.arn
  }
}

resource "aws_lb_listener_rule" "api_path_lr" {

  listener_arn = data.aws_lb_listener.https_listener.arn
  priority     = 2

  condition {
    path_pattern {
      values = ["/api/*"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lb_target_group.arn
  }
}

resource "aws_lb_listener_rule" "host_lr" {

  listener_arn = data.aws_lb_listener.https_listener.arn
  priority     = 3

  condition {
    host_header {
      values = ["nimbus.cloud.gov.bc.ca"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lb_target_group.arn
  }
}
