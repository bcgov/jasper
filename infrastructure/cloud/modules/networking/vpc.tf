data "aws_vpc" "vpc" {
  id = var.vpc_id
}

data "aws_subnets" "all_subnets" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.vpc.id]
  }
}

data "aws_subnet" "subnets" {
  for_each = toset(data.aws_subnets.all_subnets.ids)
  id       = each.key
}

locals {
  temp_web_subnets = {
    for tag_value in var.web_subnet_names :
    tag_value => [
      for subnet in data.aws_subnet.subnets :
      subnet.id if substr(subnet.tags["Name"], 0, length(tag_value)) == tag_value
    ]
  }

  web_subnets = flatten([
    for subnets in local.temp_web_subnets : subnets
  ])

  # api_subnets = {
  #   for tag_value in var.api_subnet_names :
  #   tag_value => [
  #     for subnet in data.aws_subnet.all_subnets :
  #     subnet.id if contains(subnet.tags["Name"], tag_value)
  #   ]
  # }

  # db_subnets = {
  #   for tag_value in var.db_subnet_names :
  #   tag_value => [
  #     for subnet in data.aws_subnet.all_subnets :
  #     subnet.id if contains(subnet.tags["Name"], tag_value)
  #   ]
  # }
}

# resource "aws_internet_gateway" "igw" {
#   vpc_id = data.aws_vpc.vpc.id
#   tags = {
#     Name = "${var.app_name}_igw_${var.environment}"
#   }
# }

# resource "aws_route_table" "rt" {
#   vpc_id = data.aws_vpc.vpc.id
# }

# resource "aws_route" "route" {
#   route_table_id         = aws_route_table.rt.id
#   destination_cidr_block = "0.0.0.0/0"
#   gateway_id             = aws_internet_gateway.igw.id
# }

# resource "aws_route_table_association" "rt_assoc" {
#   count          = length(var.web_subnet_names)
#   subnet_id      = local.web_subnets[count.index]
#   route_table_id = aws_route_table.rt.id
# }
