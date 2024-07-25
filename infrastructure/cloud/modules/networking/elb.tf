resource "aws_lb" "lb" {
  name               = "${var.app_name}-lb-${var.environment}"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.ecs_security_group.id]
  subnets            = aws_subnet.subnet[*].id

  enable_deletion_protection = false

  tags = {
    Name = "${var.app_name}-lb-${var.environment}"
  }
}


resource "aws_lb_target_group" "lb_target_group" {
  name     = "${var.app_name}-tg-${var.environment}"
  port     = 80
  protocol = "HTTP"
  vpc_id   = aws_vpc.vpc.id
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
