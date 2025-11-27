# Load Balancer Certificate
data "aws_acm_certificate" "default_lb_cert" {
  domain      = var.cert_domain_name
  most_recent = true
  statuses    = ["ISSUED"]
}


resource "aws_lb" "default_lb" {
  name               = "${var.environment}-app-svc-lb"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [var.web_security_group_id]
  subnets            = var.web_subnets_ids

  # Access logs are enabled and managed by LZA organizational policy
  # This block satisfies security scanning tools while allowing LZA to manage the actual configuration
  access_logs {
    bucket  = "aws-accelerator-elb-access-logs-${var.lza_log_archive_account_id}-${var.region}"
    prefix  = "${var.account_id}/elb-${var.environment}-app-svc-lb"
    enabled = true
  }

  tags = {
    Public = "True"
  }
  lifecycle {
    ignore_changes = [
      tags,
      access_logs
    ]
  }
}

# resource "aws_lb_target_group" "api_lb_tg" {
#   name        = "jasper-api-tg-${var.environment}"
#   port        = 5000
#   protocol    = "HTTP"
#   vpc_id      = var.vpc_id
#   target_type = "ip"
#   health_check {
#     enabled             = true
#     interval            = 30
#     path                = "/api/test/headers"
#     port                = 5000
#     protocol            = "HTTP"
#     healthy_threshold   = 3
#     unhealthy_threshold = 3
#     timeout             = 5
#     matcher             = "200"
#   }

#   tags = {
#     Name = "jasper-api-tg-${var.environment}"
#   }
#   lifecycle {
#     ignore_changes = [
#       tags
#     ]
#   }
# }


# resource "aws_lb_target_group" "web_lb_tg" {
#   name        = "jasper-web-tg-${var.environment}"
#   port        = 8080
#   protocol    = "HTTPS"
#   vpc_id      = var.vpc_id
#   target_type = "ip"
#   health_check {
#     enabled             = true
#     interval            = 30
#     path                = "/"
#     port                = 8080
#     protocol            = "HTTPS"
#     healthy_threshold   = 3
#     unhealthy_threshold = 3
#     timeout             = 5
#     matcher             = "200"
#   }

#   tags = {
#     Name = "jasper-web-tg-${var.environment}"
#   }
#   lifecycle {
#     ignore_changes = [
#       tags
#     ]
#   }
# }

# HTTP Listener
resource "aws_lb_listener" "http_listener" {
  load_balancer_arn = aws_lb.default_lb.arn
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
      status_code = "HTTP_301"
    }
  }
}

# HTTPS Listener
resource "aws_lb_listener" "https_listener" {
  load_balancer_arn = aws_lb.default_lb.arn
  port              = 443
  protocol          = "HTTPS"
  certificate_arn   = data.aws_acm_certificate.default_lb_cert.arn

  default_action {
    type = "fixed-response"

    fixed_response {
      content_type = "application/json"
      status_code  = 200
    }
  }
}


# health check rule
resource "aws_lb_listener_rule" "gov_healthcheck_lr" {
  listener_arn = aws_lb_listener.https_listener.arn
  priority     = 10

  condition {
    path_pattern {
      values = ["/bcgovhealthcheck"]
    }
  }

  action {
    type = "fixed-response"
    fixed_response {
      content_type = "application/json"
      status_code  = 200
    }
  }

  tags = {
    Name = "${var.app_name}-gov-healthcheck-lr-${var.environment}"
  }
}


# Web Listener Rule
resource "aws_lb_listener_rule" "web_lr" {
  listener_arn = aws_lb_listener.https_listener.arn
  priority     = 201

  condition {
    path_pattern {
      values = ["/*"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = var.tg_web_arn
  }

  tags = {
    Name = "${var.app_name}-web-lr-${var.environment}"
  }
}

# API Listener Rule
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
    target_group_arn = var.tg_api_arn
  }

  tags = {
    Name = "${var.app_name}-api-lr-${var.environment}"
  }
}

# Hangfire Listener Rule
resource "aws_lb_listener_rule" "hangfire_lr" {
  listener_arn = aws_lb_listener.https_listener.arn
  priority     = 50

  condition {
    path_pattern {
      values = ["/hangfire", "/hangfire/*"]
    }
  }

  action {
    type             = "forward"
    target_group_arn = var.tg_api_arn
  }

  tags = {
    Name = "${var.app_name}-hangfire-lr-${var.environment}"
  }
}
