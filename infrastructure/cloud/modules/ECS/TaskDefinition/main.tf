resource "aws_ecs_task_definition" "ecs_td" {
  family                   = "${var.app_name}-${var.name}-td-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory_size
  execution_role_arn       = var.ecs_execution_role_arn
  task_role_arn            = var.ecs_execution_role_arn

  # This will be uncommented out when the long term solution is implemented (JASPER-291)
  # lifecycle {
  #   # Since the dummy-image will be replaced when the GHA pipeline runs,
  #   # the whole container_definition edits has been ignored.
  #   ignore_changes = [container_definitions]
  # }

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-${var.name}-container-${var.environment}"
      image     = "${var.ecr_repository_url}:${var.image_name}" # This is a placeholder image and will be replaced every deployment of GHA.
      essential = true
      portMappings = [
        {
          containerPort = var.port
          hostPort      = var.port
          protocol      = "tcp"
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = var.log_group_name
          "awslogs-region"        = var.region
          "awslogs-stream-prefix" = "ecs"
        }
      }
      environment = var.env_variables != null ? [
        for env in var.env_variables : {
          name  = env.name
          value = env.value
        }
      ] : []
      secrets = [
        for secret in var.secret_env_variables : {
          name      = secret[0]
          valueFrom = secret[1]
        }
      ]
    }
  ])
}
