resource "aws_ecs_cluster" "ecs_cluster" {
  name = "${var.app_name}-ecs-cluster-${var.environment}"

  tags = {
    name = "${var.app_name}-ecs-cluster-${var.environment}"
  }
}

# Web
resource "aws_ecs_task_definition" "ecs_web_task_definition" {
  family                   = "${var.app_name}-web-task-definition-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512
  execution_role_arn       = var.ecs_execution_role_arn

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-web-container-${var.environment}"
      image     = "${aws_ecr_repository.ecr_repository.repository_url}:${var.app_name}-web"
      essential = true
      portMappings = [
        {
          containerPort = 8080
        }
      ],
      logConfiguration = {
        logDriver = "awslogs",
        options = {
          awslogs-group         = var.ecs_web_log_group_name,
          awslogs-region        = var.region,
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_service" "ecs_web_service" {
  name            = "${var.app_name}-ecs-web-service-${var.environment}"
  cluster         = aws_ecs_cluster.ecs_cluster.id
  task_definition = aws_ecs_task_definition.ecs_web_task_definition.arn
  launch_type     = "FARGATE"
  desired_count   = 1

  network_configuration {
    subnets          = var.subnet_ids
    security_groups  = [var.sg_id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = var.lb_tg_arn
    container_name   = "${var.app_name}-web-container-${var.environment}"
    container_port   = 8080
  }
}
