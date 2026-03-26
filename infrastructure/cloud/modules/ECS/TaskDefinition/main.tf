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

  container_definitions = jsonencode(concat(
    [
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
        mountPoints = var.efs_volume_config != null ? [
          {
            sourceVolume  = var.efs_volume_config.name
            containerPath = var.efs_volume_config.container_path
            readOnly      = false
          }
        ] : []
        dependsOn = var.clamav_config != null ? [
          {
            containerName = "${var.app_name}-clamav-${var.environment}"
            condition     = "HEALTHY"
          }
        ] : []
      }
    ],
    var.clamav_config != null ? [
      {
        name              = "${var.app_name}-clamav-${var.environment}"
        image             = var.clamav_config.image
        essential         = false
        memoryReservation = var.clamav_config.memory_reservation
        portMappings = [
          {
            containerPort = var.clamav_config.port
            hostPort      = var.clamav_config.port
            protocol      = "tcp"
          }
        ]
        environment = [
          {
            name  = "CLAMAV_NO_FRESHCLAMD"
            value = "false"
          },
          {
            name  = "CLAMD_CONF_StreamMaxLength"
            value = var.clamav_config.stream_max_length
          }
        ]
        logConfiguration = {
          logDriver = "awslogs"
          options = {
            "awslogs-group"         = var.log_group_name
            "awslogs-region"        = var.region
            "awslogs-stream-prefix" = "clamav"
          }
        }
        healthCheck = {
          command     = ["CMD", "/usr/local/bin/clamdcheck.sh"]
          interval    = 30
          timeout     = 10
          retries     = 3
          startPeriod = 120
        }
        mountPoints = []
        volumesFrom = []
      }
    ] : []
  ))

  dynamic "volume" {
    for_each = var.efs_volume_config != null ? [var.efs_volume_config] : []
    content {
      name = volume.value.name

      efs_volume_configuration {
        file_system_id     = volume.value.file_system_id
        transit_encryption = "ENABLED"

        dynamic "authorization_config" {
          for_each = volume.value.access_point_id != null ? [1] : []
          content {
            access_point_id = volume.value.access_point_id
            iam             = "ENABLED"
          }
        }
      }
    }
  }
}
