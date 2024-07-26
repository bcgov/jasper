resource "aws_ecs_cluster" "ecs_cluster" {
  name = "${var.app_name}-cluster-${var.environment}"

  tags = {
    name = "${var.app_name}-cluster-${var.environment}"
  }
}

# Web
resource "aws_ecs_task_definition" "ecs_web_task_definition" {
  family                   = "${var.app_name}-web-task-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-web-container-${var.environment}"
      image     = "${aws_ecr_repository.ecr_repository.repository_url}:${var.web_image_name}"
      essential = true
      portMappings = [
        {
          containerPort = var.web_port
          hostPort      = var.web_port
        }
      ]
    }
  ])

  execution_role_arn = var.ecs_task_execution_iam_role_arn
  task_role_arn      = var.ecs_task_execution_iam_role_arn
}

resource "aws_ecs_service" "ecs_web_service" {
  name            = "${var.app_name}-service-${var.environment}"
  cluster         = aws_ecs_cluster.ecs_cluster.id
  task_definition = aws_ecs_task_definition.ecs_web_task_definition.arn
  launch_type     = "FARGATE"
  desired_count   = 1

  network_configuration {
    subnets          = var.subnet_id
    security_groups  = [var.ecs_sg_id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = var.lb_tg_arn
    container_name   = "${var.app_name}-web-container-${var.environment}"
    container_port   = var.web_port
  }

  depends_on = [var.lb_listener]
}
