output "web_subnets_ids" {
  value = data.aws_subnets.web.ids
  
}

output "app_subnets_ids" {
  value = data.aws_subnets.app.ids
}

output "data_subnets_ids" {
  value = data.aws_subnets.data.ids
}

output "all_subnet_ids" {
  value = concat(
    data.aws_subnets.web.ids,
    data.aws_subnets.app.ids,
    data.aws_subnets.data.ids
  )
}
