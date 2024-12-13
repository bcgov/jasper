output "web_subnets_ids" {
  value = [for subnet in local.web_subnets : subnet.id]
}

output "app_subnets_ids" {
  value = [for subnet in local.app_subnets : subnet.id]
}

output "data_subnets_ids" {
  value = [for subnet in local.data_subnets : subnet.id]
}

output "all_subnet_ids" {
  value = concat(
    [for subnet in local.web_subnets : subnet.id],
    [for subnet in local.app_subnets : subnet.id],
    [for subnet in local.data_subnets : subnet.id]
  )
}
