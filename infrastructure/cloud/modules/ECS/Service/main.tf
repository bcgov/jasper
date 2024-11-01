resource "aws_ecs_service" "ecs_service" {
  name            = "${var.app_name}-${var.name}-ecs-service-${var.environment}"
  cluster         = var.ecs_cluster_id
  task_definition = var.ecs_td_arn
  launch_type     = "FARGATE"
  desired_count   = 1

  network_configuration {
    subnets          = var.subnet_ids
    security_groups  = [var.sg_id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = var.tg_arn
    container_name   = "${var.app_name}-${var.name}-container-${var.environment}"
    container_port   = var.port
  }

  lifecycle {
    prevent_destroy = true
  }
}
