resource "aws_ecs_service" "ecs_service" {
  name            = "${var.app_name}-${var.name}-ecs-service-${var.environment}"
  cluster         = var.ecs_cluster_id
  task_definition = var.ecs_td_arn
  launch_type     = "FARGATE"
  desired_count   = var.min_capacity

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
    ignore_changes  = [desired_count]
  }
}

# Auto Scaling Target
resource "aws_appautoscaling_target" "ecs_target" {
  max_capacity       = var.max_capacity
  min_capacity       = var.min_capacity
  resource_id        = "service/${var.ecs_cluster_name}/${aws_ecs_service.ecs_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

# Auto Scaling Policy - Scale Up
resource "aws_appautoscaling_policy" "ecs_policy_up" {
  name               = "${var.app_name}-${var.name}-scale-up-${var.environment}"
  policy_type        = "StepScaling"
  resource_id        = aws_appautoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace

  step_scaling_policy_configuration {
    adjustment_type         = "ChangeInCapacity"
    cooldown                = 60
    metric_aggregation_type = "Maximum"

    step_adjustment {
      metric_interval_lower_bound = 0
      scaling_adjustment          = 1
    }
  }
}

# Auto Scaling Policy - Scale Down
resource "aws_appautoscaling_policy" "ecs_policy_down" {
  name               = "${var.app_name}-${var.name}-scale-down-${var.environment}"
  policy_type        = "StepScaling"
  resource_id        = aws_appautoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace

  step_scaling_policy_configuration {
    adjustment_type         = "ChangeInCapacity"
    cooldown                = 60
    metric_aggregation_type = "Maximum"

    step_adjustment {
      metric_interval_upper_bound = 0
      scaling_adjustment          = -1
    }
  }
}
