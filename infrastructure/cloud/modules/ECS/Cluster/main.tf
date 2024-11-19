resource "aws_ecs_cluster" "ecs_cluster" {
  name = "${var.app_name}-${var.name}-cluster-${var.environment}"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = {
    name = "${var.app_name}-${var.name}-cluster-${var.environment}"
  }
}
