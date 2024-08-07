resource "aws_lb" "lb" {
  name                       = "${var.app_name}-lb-${var.environment}"
  internal                   = false
  load_balancer_type         = "application"
  subnets                    = var.subnet_ids
  security_groups            = [aws_security_group.sg.id]
  enable_deletion_protection = false

  tags = {
    Name = "${var.app_name}-lb-${var.environment}"
  }
}


resource "aws_lb_target_group" "lb_target_group" {
  name        = "${var.app_name}-lb-tg-${var.environment}"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = data.aws_vpc.default.id
  target_type = "ip"

  health_check {
    port                = 8080
    healthy_threshold   = 3
    unhealthy_threshold = 2
    timeout             = 3
    interval            = 30
    path                = "/"
    protocol            = "HTTP"
    matcher             = "200"
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
