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

  temp_app_subnets = {
    for tag_value in var.app_subnet_names :
    tag_value => [
      for subnet in data.aws_subnet.subnets :
      subnet.id if substr(subnet.tags["Name"], 0, length(tag_value)) == tag_value
    ]
  }

  app_subnets = flatten([
    for subnets in local.temp_app_subnets : subnets
  ])

  temp_data_subnets = {
    for tag_value in var.data_subnet_names :
    tag_value => [
      for subnet in data.aws_subnet.subnets :
      subnet.id if substr(subnet.tags["Name"], 0, length(tag_value)) == tag_value
    ]
  }

  data_subnets = flatten([
    for subnets in local.temp_data_subnets : subnets
  ])
}
