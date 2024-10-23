resource "aws_db_instance" "postgres_db_instance" {
  allocated_storage      = 20
  storage_type           = "gp2"
  engine                 = "postgres"
  engine_version         = "16.3"
  instance_class         = "db.t3.micro"
  db_name                = "${var.app_name}postgresdb${var.environment}"
  username               = var.db_username
  password               = var.db_password
  parameter_group_name   = "default.postgres16"
  vpc_security_group_ids = [var.data_sg_id]
  db_subnet_group_name   = "default-${var.vpc_id}"
  storage_encrypted      = true
  kms_key_id             = var.kms_key_arn
  ca_cert_identifier     = var.rds_db_ca_cert
}
