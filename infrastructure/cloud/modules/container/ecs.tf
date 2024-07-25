resource "aws_ecs_cluster" "ecs_cluster" {
  name = "${var.app_name}-cluster-${var.environment}"

  tags = {
    name = "${var.app_name}-cluster-${var.environment}"
  }
}

resource "aws_ecs_task_definition" "ecs_task_definition" {
  family                   = "${var.app_name}-task-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-container-${var.environment}"
      image     = "${aws_ecr_repository.ecr_repository.repository_url}:latest"
      essential = true
      portMappings = [
        {
          containerPort = 80
          hostPort      = 80
        }
      ]
    }
  ])

  execution_role_arn = module.security.ecs_task_execution_iam_role_arn
  task_role_arn      = module.security.ecs_task_execution_iam_role_arn
}

resource "aws_ecs_service" "ecs_service" {
  name            = "${var.app_name}-service-${var.environment}"
  cluster         = aws_ecs_cluster.ecs_cluster.id
  task_definition = aws_ecs_task_definition.ecs_task_definition.arn
  launch_type     = "FARGATE"
  desired_count   = 1

  network_configuration {
    subnets          = module.networking.subnet_private_id
    security_groups  = [module.networking.ecs_sg_id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = module.networking.lb_tg_arn
    container_name   = "${var.app_name}-container-${var.environment}"
    container_port   = 80
  }

  depends_on = [module.networking.lb_listener]
}
