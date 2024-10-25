# Load Balancer Security Group	
resource "aws_security_group" "sg" {
  name        = "${var.app_name}-lb-sg-${var.environment}"
  vpc_id      = data.aws_vpc.vpc.id
  description = "May change once Network Architecture has been finalized."

  ingress {
    description = "Allow inbound HTTP traffic on port 80"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "Accept traffic on port 8080"
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "Unrestricted"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.app_name}_sg_${var.environment}"
  }
}


# ECS Security Group	
resource "aws_security_group" "ecs_sg" {
  name        = "${var.app_name}-ecs-sg-${var.environment}"
  vpc_id      = data.aws_vpc.vpc.id
  description = "May change once Network Architecture has been finalized."

  ingress {
    description     = "Accept traffic on port 8080 and from specific Security Group"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    cidr_blocks     = null
    security_groups = [aws_security_group.sg.id]
  }

  egress {
    description = "Unrestricted"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}
