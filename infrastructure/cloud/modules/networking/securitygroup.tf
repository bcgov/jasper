data "aws_security_group" "sg" {
  vpc_id = data.aws_vpc.vpc.id
  name   = "Web_sg"
}
