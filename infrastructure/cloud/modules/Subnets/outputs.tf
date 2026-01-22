# output "web_subnets_ids" {
#   value = data.aws_subnets.web.ids

# }

# output "app_subnets_ids" {
#   value = data.aws_subnets.app.ids
# }

# output "data_subnets_ids" {
#   value = data.aws_subnets.data.ids
# }

# output "all_subnet_ids" {
#   value = concat(
#     data.aws_subnets.web.ids,
#     data.aws_subnets.app.ids,
#     data.aws_subnets.data.ids
#   )
# }

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
