resource "aws_vpc" "vpc" {
  cidr_block = var.vpc_cidr
}

resource "aws_internet_gateway" "internet_gateway" {
  vpc_id = aws_vpc.vpc.id
}

resource "aws_subnet" "subnet" {
  count                   = length(var.private_subnets)
  vpc_id                  = aws_vpc.vpc.id
  cidr_block              = element(var.private_subnets, count.index)
  map_public_ip_on_launch = false
}

resource "aws_route_table" "route_table" {
  vpc_id = aws_vpc.vpc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.internet_gateway.id
  }
}

resource "aws_route_table_association" "route_table_association" {
  count          = length(var.private_subnets)
  subnet_id      = element(aws_subnet.subnet.*.id, count.index)
  route_table_id = aws_route_table.route_table.id
}


resource "aws_security_group" "lb_sg" {
  name   = "${var.app_name}-lb-sg-${var.environment}"
  vpc_id = aws_vpc.vpc.id

  ingress {
    protocol    = "tcp"
    from_port   = 80
    to_port     = 80
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }
}


resource "aws_security_group" "ecs_security_group" {
  name   = "${var.app_name}-ecs-sg-${var.environment}"
  vpc_id = aws_vpc.vpc.id

  ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.lb_sg.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}
