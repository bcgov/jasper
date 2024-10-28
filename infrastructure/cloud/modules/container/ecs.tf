resource "aws_ecs_cluster" "ecs_cluster" {
  name = "${var.app_name}-ecs-cluster-${var.environment}"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

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
  task_role_arn            = var.ecs_execution_role_arn

  lifecycle {
    ignore_changes = [container_definitions]
  }

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-web-container-${var.environment}"
      image     = "${aws_ecr_repository.ecr_repository.repository_url}:jasper-web"
      essential = true
      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
          protocol      = "tcp"
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = var.ecs_web_td_log_group_name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs"
        }
      }
      secrets = [
        for secret in var.web_secrets : {
          name      = secret[0]
          valueFrom = secret[1]
        }
      ]
    }
  ])
}

resource "aws_ecs_service" "ecs_web_service" {
  name                   = "${var.app_name}-ecs-web-service-${var.environment}"
  cluster                = aws_ecs_cluster.ecs_cluster.id
  task_definition        = aws_ecs_task_definition.ecs_web_task_definition.arn
  launch_type            = "FARGATE"
  desired_count          = 1
  enable_execute_command = true

  network_configuration {
    subnets          = var.web_subnet_ids
    security_groups  = [var.app_sg_id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = var.web_tg_arn
    container_name   = "${var.app_name}-web-container-${var.environment}"
    container_port   = 8080
  }

  lifecycle {
    prevent_destroy = true
  }
}

# API
resource "aws_ecs_task_definition" "ecs_api_task_definition" {
  family                   = "${var.app_name}-api-task-definition-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512
  execution_role_arn       = var.ecs_execution_role_arn
  task_role_arn            = var.ecs_execution_role_arn

  lifecycle {
    ignore_changes = [container_definitions]
  }

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-api-container-${var.environment}"
      image     = "${aws_ecr_repository.ecr_repository.repository_url}:jasper-api"
      essential = true
      portMappings = [
        {
          containerPort = 5000
          hostPort      = 5000
          protocol      = "tcp"
        }
      ]
      environment = [
        {
          name  = "CORS_DOMAIN"
          value = var.default_lb_dns_name
        }
      ]
      secrets = [
        for secret in var.api_secrets : {
          name      = secret[0]
          valueFrom = secret[1]
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = var.ecs_api_td_log_group_name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_service" "ecs_api_service" {
  name            = "${var.app_name}-ecs-api-service-${var.environment}"
  cluster         = aws_ecs_cluster.ecs_cluster.id
  task_definition = aws_ecs_task_definition.ecs_api_task_definition.arn
  launch_type     = "FARGATE"
  desired_count   = 1

  network_configuration {
    subnets          = var.app_subnet_ids
    security_groups  = [var.app_sg_id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = var.api_tg_arn
    container_name   = "${var.app_name}-api-container-${var.environment}"
    container_port   = 5000
  }

  lifecycle {
    prevent_destroy = true
  }
}
