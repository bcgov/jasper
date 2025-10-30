output "efs_id" {
  description = "The EFS file system ID"
  value       = aws_efs_file_system.efs.id
}
