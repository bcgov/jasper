output "web_subnets_ids" {
  value = [for subnet in local.web_subnets : subnet.id]
}

output "app_subnets_ids" {
  value = [for subnet in local.app_subnets : subnet.id]
}

output "data_subnets_ids" {
  value = [for subnet in local.data_subnets : subnet.id]
}
