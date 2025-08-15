locals {
  ip_list = split(",", var.allowed_ip_ranges)
}

resource "aws_wafv2_ip_set" "waf_ip_set" {
  name               = "${var.app_name}-bcgov-ip-set-${var.environment}"
  description        = "BC Gov CIDRs ranges to restrict JASPER access to users connected to VPN only"
  scope              = "REGIONAL"
  ip_address_version = "IPV4"
  addresses          = local.ip_list
}

resource "aws_wafv2_web_acl" "waf_web_acl" {
  name        = "${var.app_name}-waf-web-acl-${var.environment}"
  description = "Enforces strict access control by permitting only requests from the BC Gov CIDRs ranges and health check. Other requests are blocked."
  scope       = "REGIONAL"

  default_action {
    block {}
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "lb-waf-web-acl-metric"
    sampled_requests_enabled   = true
  }

  rule {
    name     = "${var.app_name}-allow-healthcheck-rule-${var.environment}"
    priority = 1

    statement {
      byte_match_statement {
        field_to_match {
          uri_path {
          }
        }

        positional_constraint = "EXACTLY"
        search_string         = "/bcgovhealthcheck"

        text_transformation {
          priority = 0
          type     = "NONE"
        }
      }
    }

    action {
      allow {}
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "allow-healtcheck-rule-metric"
      sampled_requests_enabled   = true
    }
  }

  rule {
    name     = "${var.app_name}-allow-bcgov-ips-rule-${var.environment}"
    priority = 2

    statement {
      ip_set_reference_statement {
        arn = aws_wafv2_ip_set.waf_ip_set.arn

        ip_set_forwarded_ip_config {
          header_name       = "X-Forwarded-For"
          position          = "ANY"
          fallback_behavior = "NO_MATCH"
        }
      }
    }

    action {
      allow {}
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "allow-bcgov-ips-rule-metric"
      sampled_requests_enabled   = true
    }
  }
}

resource "aws_wafv2_web_acl_association" "waf_web_acl_assoc" {
  resource_arn = var.default_lb_arn
  web_acl_arn  = aws_wafv2_web_acl.waf_web_acl.arn
}

resource "aws_wafv2_web_acl_logging_configuration" "waf_logging_config" {
  resource_arn            = aws_wafv2_web_acl.waf_web_acl.arn
  log_destination_configs = [var.log_group_arn]
}
