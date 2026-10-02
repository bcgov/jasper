

resource "aws_docdb_cluster_parameter_group" "mongo_params" {
  family = "docdb5.0"
  name   = "${var.app_name}-mongo-param-val-${var.environment}"

  parameter {
    name  = "tls"
    value = "tls1.2+"
  }
}


resource "aws_docdb_subnet_group" "mongo_grp" {
  name       = "${var.app_name}-mongo-subnet-group-${var.environment}"
  subnet_ids = var.data_subnets_ids
}


resource "aws_docdb_cluster" "mongo_cluster" {
  skip_final_snapshot             = true
  db_subnet_group_name            = aws_docdb_subnet_group.mongo_grp.name
  cluster_identifier              = "${var.app_name}-mongo-app-cluster-${var.environment}"
  engine                          = "docdb"
  master_username                 = var.mongousername
  manage_master_user_password     = true
  db_cluster_parameter_group_name = aws_docdb_cluster_parameter_group.mongo_params.name
  storage_encrypted               = true
  kms_key_id                      = var.kms_key_id
  vpc_security_group_ids          = [var.app_sg_id]
  storage_type                    = "iopt1"
  deletion_protection             = var.delete_protection_enabled
  apply_immediately               = true
  backup_retention_period         = 1
  enabled_cloudwatch_logs_exports = ["audit", "profiler"]

}


resource "aws_docdb_cluster_instance" "mongo_instance" {
  count              = var.mongo_node_count
  identifier         = "${var.app_name}-mongo-instance-${var.environment}"
  cluster_identifier = aws_docdb_cluster.mongo_cluster.id
  instance_class     = var.mongo_instance_type
}
