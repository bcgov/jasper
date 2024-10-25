#
# BCGOV provisioned security groups
#
data "aws_security_group" "web_sg" {
  name = "Web_sg"
}

data "aws_security_group" "app_sg" {
  name = "App_sg"
}

data "aws_security_group" "data_sg" {
  name = "Data_sg"
}

# #
# # Load Balancer Security Group
# #
# resource "aws_security_group" "lb_sg" {
#   name        = "${var.app_name}-lb-sg-${var.environment}"
#   vpc_id      = data.aws_vpc.vpc.id
#   description = "Security Group for the Application Load Balancer"

#   tags = {
#     Name = "${var.app_name}_lb_sg_${var.environment}"
#   }
# }

# # Load Balancer Ingress Rules. This will change once we get the public load balancer details from cloud team.
# resource "aws_vpc_security_group_ingress_rule" "lb_sg_ingress_http_allow_80" {
#   security_group_id = aws_security_group.lb_sg.id
#   description       = "Allow inbound HTTP traffic on port 80"
#   ip_protocol       = "tcp"
#   from_port         = 80
#   to_port           = 80
#   cidr_ipv4         = "0.0.0.0/0"
# }

# resource "aws_vpc_security_group_ingress_rule" "lb_sg_ingress_http_allow_8080" {
#   security_group_id = aws_security_group.lb_sg.id
#   description       = "Allow inbound HTTP traffic on port 8080"
#   ip_protocol       = "tcp"
#   from_port         = 8080
#   to_port           = 8080
#   cidr_ipv4         = "0.0.0.0/0"
# }

# # Load Balancer Egress Rules
# resource "aws_vpc_security_group_egress_rule" "lb_sg_egress_allow_to_ecs_sg" {
#   security_group_id            = aws_security_group.lb_sg.id
#   referenced_security_group_id = aws_security_group.ecs_sg.id
#   description                  = "Allow all outbound traffic to ECS SG from Load Balancer SG"
#   ip_protocol                  = "-1"
# }

# #
# # ECS Security Group
# #
# resource "aws_security_group" "ecs_sg" {
#   name        = "${var.app_name}-ecs-sg-${var.environment}"
#   vpc_id      = data.aws_vpc.vpc.id
#   description = "Security Group for ECS services"

#   tags = {
#     Name = "${var.app_name}_ecs_sg_${var.environment}"
#   }
# }

# # ECS Ingress Rules
# # Remove ecs_sg_ingress_allow_icmp and ecs_sg_ingress_allow_ssh once the JASPER
# # is publicly accessible. These ingress rules is for tesing SG-SG connectivity using
# # EC2 Instance and EC2 Instance Connect Endpoint 
# resource "aws_vpc_security_group_ingress_rule" "ecs_sg_ingress_allow_from_web_sg" {
#   security_group_id            = aws_security_group.ecs_sg.id
#   referenced_security_group_id = data.aws_security_group.web_sg.id
#   description                  = "Allow all inbound traffic from Web SG"
#   ip_protocol                  = -1
# }

# resource "aws_vpc_security_group_ingress_rule" "ecs_sg_ingress_allow_from_lambda_sg" {
#   security_group_id            = aws_security_group.ecs_sg.id
#   referenced_security_group_id = aws_security_group.lambda_sg.id
#   description                  = "Allow all inbound traffic from Lambda SG"
#   ip_protocol                  = -1
# }

# resource "aws_vpc_security_group_ingress_rule" "ecs_sg_ingress_allow_icmp" {
#   security_group_id = aws_security_group.ecs_sg.id
#   description       = "Allow inbound ICMP traffic to ECS SG to allow pinging the Lambda SG"
#   ip_protocol       = "icmp"
#   from_port         = -1
#   to_port           = -1
#   cidr_ipv4         = "0.0.0.0/0"
# }

# resource "aws_vpc_security_group_ingress_rule" "ecs_sg_ingress_allow_ssh" {
#   security_group_id = aws_security_group.ecs_sg.id
#   description       = "Allow inbound SSH traffic to ECS SG"
#   ip_protocol       = "tcp"
#   from_port         = 22
#   to_port           = 22
#   cidr_ipv4         = data.aws_vpc.vpc.cidr_block
# }

# # ECS Egress Rules
# resource "aws_vpc_security_group_egress_rule" "ecs_sg_egress_allow_to_anywhere" {
#   security_group_id = aws_security_group.ecs_sg.id
#   description       = "Unrestricted"
#   ip_protocol       = "-1"
#   cidr_ipv4         = "0.0.0.0/0"
# }

# #
# # Lambda Security Group
# #
# resource "aws_security_group" "lambda_sg" {
#   name        = "${var.app_name}-lambda-sg-${var.environment}"
#   vpc_id      = data.aws_vpc.vpc.id
#   description = "Security Group for Lambda functions"

#   tags = {
#     Name = "${var.app_name}_lambda_sg_${var.environment}"
#   }
# }

# # Lambda Ingress Rules
# # Remove lambda_sg_ingress_allow_icmp and lambda_sg_ingress_allow_ssh once the JASPER
# # is publicly accessible. These ingress rules is for tesing SG-SG connectivity using
# # EC2 Instance and EC2 Instance Connect Endpoint 
# resource "aws_vpc_security_group_ingress_rule" "lambda_sg_ingress_allow_from_ecs_sg" {
#   security_group_id            = aws_security_group.lambda_sg.id
#   referenced_security_group_id = aws_security_group.ecs_sg.id
#   description                  = "Allow all inbound traffic from ECS SG"
#   ip_protocol                  = -1
# }

# resource "aws_vpc_security_group_ingress_rule" "lambda_sg_ingress_allow_icmp" {
#   security_group_id = aws_security_group.lambda_sg.id
#   description       = "Allow inbound ICMP traffic to Lambda SG to allow pinging the ECS SG"
#   ip_protocol       = "icmp"
#   from_port         = -1
#   to_port           = -1
#   cidr_ipv4         = "0.0.0.0/0"
# }

# resource "aws_vpc_security_group_ingress_rule" "lambda_sg_ingress_allow_ssh" {
#   security_group_id = aws_security_group.lambda_sg.id
#   description       = "Allow inbound SSH traffic to Lambda SG"
#   ip_protocol       = "tcp"
#   from_port         = 22
#   to_port           = 22
#   cidr_ipv4         = data.aws_vpc.vpc.cidr_block
# }

# # Lambda Egress Rules
# resource "aws_vpc_security_group_egress_rule" "lambda_sg_egress_allow_to_anywhere" {
#   security_group_id = aws_security_group.lambda_sg.id
#   description       = "Unrestricted"
#   ip_protocol       = "-1"
#   cidr_ipv4         = "0.0.0.0/0"
# }
