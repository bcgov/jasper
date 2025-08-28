data "aws_security_group" "data_sg" {
  name = "Data"
}

resource "aws_db_subnet_group" "db_subnet_group" {
  name        = "${var.app_name}-db-subnet-group-${var.environment}"
  description = "DB Subnet group for RDS instance"
  subnet_ids  = var.all_subnet_ids
}

resource "aws_db_instance" "postgres_db_instance" {
  allocated_storage                   = 20
  storage_type                        = "gp2"
  engine                              = "postgres"
  engine_version                      = "16.8"
  instance_class                      = "db.t3.micro"
  db_name                             = "${var.app_name}postgresdb${replace(var.environment, "-", "")}"
  username                            = var.db_username
  password                            = var.db_password
  parameter_group_name                = "default.postgres16"
  vpc_security_group_ids              = [data.aws_security_group.data_sg.id]
  db_subnet_group_name                = aws_db_subnet_group.db_subnet_group.name
  storage_encrypted                   = true
  kms_key_id                          = var.kms_key_arn
  ca_cert_identifier                  = var.rds_db_ca_cert
  identifier                          = "${var.app_name}-postgres-db-${replace(var.environment, "-", "")}"
  skip_final_snapshot                 = true
  backup_retention_period             = 0 #tfsec:ignore:aws-rds-specify-backup-retention
  performance_insights_enabled        = true
  performance_insights_kms_key_id     = var.kms_key_arn
  iam_database_authentication_enabled = true
  deletion_protection                 = true
  apply_immediately                   = true
}
