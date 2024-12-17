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
  description = "Load Balancer Web Application Firewall"
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
    name     = "${var.app_name}-allow-bcgov-ips-rule-${var.environment}"
    priority = 1

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
