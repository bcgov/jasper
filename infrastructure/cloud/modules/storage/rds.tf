resource "aws_db_instance" "postgres_db_instance" {
  allocated_storage      = 20
  storage_type           = "gp2"
  engine                 = "postgres"
  engine_version         = "16.3"
  instance_class         = "db.t3.micro"
  db_name                = "${var.app_name}-postgres-db-${var.environment}"
  username               = var.db_username
  password               = var.db_password
  parameter_group_name   = "default.postgres16"
  vpc_security_group_ids = [var.data_sg_id]
}
