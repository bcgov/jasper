output "efs_id" {
  description = "The EFS file system ID"
  value       = aws_efs_file_system.efs.id
}

output "efs_arn" {
  description = "The EFS file system ARN"
  value       = aws_efs_file_system.efs.arn
}

output "efs_dns_name" {
  description = "The DNS name of the EFS file system"
  value       = aws_efs_file_system.efs.dns_name
}

output "access_point_id" {
  description = "The EFS access point ID"
  value       = aws_efs_access_point.access_point.id
}

output "access_point_arn" {
  description = "The EFS access point ARN"
  value       = aws_efs_access_point.access_point.arn
}
