<<<<<<< HEAD
resource "aws_vpc" "vpc" {
  cidr_block           = "10.120.0.0/16"
  instance_tenancy     = "default"
  enable_dns_hostnames = true
  enable_dns_support   = true
  tags = {
    Name = "${var.app_name}_vpc_${var.environment}"
  }
}

data "aws_availability_zones" "az_availables" {
  state = "available"
}

resource "aws_subnet" "public_subnets" {
  count                   = 2
  availability_zone       = data.aws_availability_zones.az_availables.names[count.index]
  vpc_id                  = aws_vpc.vpc.id
  cidr_block              = cidrsubnet(aws_vpc.vpc.cidr_block, 7, count.index + 1)
  map_public_ip_on_launch = true
  tags = {
    Name = "${var.app_name}_public_subnet_${count.index}_${var.environment}"
  }
}

resource "aws_subnet" "private_subnets_web" {
  count             = 2
  availability_zone = data.aws_availability_zones.az_availables.names[count.index]
  vpc_id            = aws_vpc.vpc.id
  cidr_block        = cidrsubnet(aws_vpc.vpc.cidr_block, 7, count.index + 3)
  tags = {
    Name = "${var.app_name}_private_subnet_web_${count.index}_${var.environment}"
  }
}

resource "aws_internet_gateway" "igw" {
  vpc_id = aws_vpc.vpc.id
  tags = {
    Name = "${var.app_name}_igw_${var.environment}"
  }
}

resource "aws_default_route_table" "rt_public" {
  default_route_table_id = aws_vpc.vpc.default_route_table_id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.igw.id
  }

  tags = {
    Name = "${var.app_name}_public_rt_${var.environment}"
  }
}

resource "aws_eip" "eip" {
  domain = "vpc"
  tags = {
    Name = "${var.app_name}_eip_${var.environment}"
  }
}

resource "aws_nat_gateway" "natgw" {
  allocation_id = aws_eip.eip.id
  subnet_id     = aws_subnet.public_subnets[0].id
  tags = {
    Name = "${var.app_name}_nat_${var.environment}"
  }
}

resource "aws_route_table" "rt_private" {
  vpc_id = aws_vpc.vpc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_nat_gateway.natgw.id
  }

  tags = {
    Name = "${var.app_name}_private_rt_${var.environment}"
  }
}

resource "aws_route_table_association" "rt_assoc_priv_subnets_web" {
  count          = 2
  subnet_id      = aws_subnet.private_subnets_web[count.index].id
  route_table_id = aws_route_table.rt_private.id
}

resource "aws_route_table_association" "rt_assoc_pub_subnets" {
  count          = 2
  subnet_id      = aws_subnet.public_subnets[count.index].id
  route_table_id = aws_vpc.vpc.main_route_table_id
}
=======
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default_public" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }

  filter {
    name   = "default-for-az"
    values = ["true"]
  }
}
>>>>>>> master
