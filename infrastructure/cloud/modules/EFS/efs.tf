resource "aws_efs_file_system" "efs_fs" {
  creation_token = "jasper-efs-${var.environment}"
  encrypted      = true
  kms_key_id     = var.kms_key_arn
  tags = {
    Name = "jasper-efs-${var.environment}"
  }
}


# mount points for app subnets
resource "aws_efs_mount_target" "app_efs_mounts" {
  for_each = toset(var.app_subnet_ids)
  file_system_id = aws_efs_file_system.efs_fs.id
  subnet_id      = each.value
  security_groups = [var.app_security_group_id]
}

# mount points for web subnets
resource "aws_efs_mount_target" "web_efs_mounts" {
  for_each = toset(var.web_subnet_ids)
  file_system_id = aws_efs_file_system.efs_fs.id
  subnet_id      = each.value
  security_groups = [var.web_security_group_id]
}