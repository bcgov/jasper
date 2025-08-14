output "web_subnets_ids" {
  value = [for subnet in data.aws_subnets.web : subnet.id]
}

output "app_subnets_ids" {
  value = [for subnet in data.aws_subnets.app : subnet.id]
}

output "data_subnets_ids" {
  value = [for subnet in data.aws_subnets.data : subnet.id]
}

output "all_subnet_ids" {
  value = concat(
    [for subnet in data.aws_subnets.web : subnet.id],
    [for subnet in data.aws_subnets.app : subnet.id],
    [for subnet in data.aws_subnets.data : subnet.id]
  )
}
