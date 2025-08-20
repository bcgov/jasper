

output efs_fs_id {
  description = "The ID of the EFS file system"
  value       = aws_efs_file_system.efs_fs.id
}