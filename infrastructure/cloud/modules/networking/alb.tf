#
# Default Load Balancer
#
data "aws_lb" "default_lb" {
  name = var.lb_name
}

# HTTP Listener
resource "aws_lb_listener" "http_listener" {
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

# HTTPS Listener
resource "aws_lb_listener" "https_listener" {
  load_balancer_arn = data.aws_lb.default_lb.arn
  port              = 443
  protocol          = "HTTPS"
  certificate_arn   = var.default_lb_cert_arn

  default_action {
    type = "fixed-response"

    fixed_response {
      content_type = "application/json"
      status_code  = 200
    }
  }
}

# HTTPS Listener Rules
resource "aws_lb_listener_rule" "web_lr" {
  listener_arn = aws_lb_listener.https_listener.arn
  priority     = 200

  condition {
    path_pattern {
      values = ["/*"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.web_target_group.arn
  }

  tags = {
    Name = "${var.app_name}-web-lr-${var.environment}"
  }
}

resource "aws_lb_listener_rule" "api_path_lr" {
  listener_arn = aws_lb_listener.https_listener.arn
  priority     = 100

  condition {
    path_pattern {
      values = ["/api/*"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api_target_group.arn
  }

  tags = {
    Name = "${var.app_name}-api-lr-${var.environment}"
  }
}

