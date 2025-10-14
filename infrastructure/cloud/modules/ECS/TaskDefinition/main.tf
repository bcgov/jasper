// Retrieves the latest deployed image name (web or api) from SSM Parameter Store.
// Initial value is 'dummy-image' but gets replaced by Web and API GHA. It was agreed
// that the parameter is created manually via Console for DEV, TEST and PROD.
data "aws_ssm_parameter" "image_param" {
  name = "/images/${var.app_name}-${var.name}-image-param-${var.environment}"
}

resource "aws_ecs_task_definition" "ecs_td" {
  family                   = "${var.app_name}-${var.name}-td-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.cpu
  memory                   = var.memory_size
  execution_role_arn       = var.ecs_execution_role_arn
  task_role_arn            = var.ecs_execution_role_arn

  container_definitions = jsonencode([
    {
      name      = "${var.app_name}-${var.name}-container-${var.environment}"
      image     = "${var.ecr_repository_url}:${data.aws_ssm_parameter.image_param.value}"
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
