resource "aws_efs_file_system" "efs" {
  creation_token = "${var.app_name}-${var.name}-efs-${var.environment}"
  encrypted      = true
  kms_key_id     = var.kms_key_arn

  lifecycle_policy {
    transition_to_ia = "AFTER_30_DAYS"
  }

  tags = {
    Name        = "${var.app_name}-${var.name}-efs-${var.environment}"
    Environment = var.environment
    Purpose     = var.purpose
  }
}

resource "aws_efs_mount_target" "mount_target" {
  count = length(var.subnet_ids)

  file_system_id  = aws_efs_file_system.efs.id
  subnet_id       = var.subnet_ids[count.index]
  security_groups = var.security_group_ids
}

resource "aws_efs_access_point" "access_point" {
  file_system_id = aws_efs_file_system.efs.id

  root_directory {
    path = "/${var.name}"
    creation_info {
      owner_gid   = 1000
      owner_uid   = 1000
      permissions = "755"
    }
  }

  posix_user {
    gid = 1000
    uid = 1000
  }

  tags = {
    Name        = "${var.app_name}-${var.name}-efs-ap-${var.environment}"
    Environment = var.environment
  }
}
