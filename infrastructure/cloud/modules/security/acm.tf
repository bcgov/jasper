data "aws_acm_certificate" "default_lb_cert" {
  domain      = var.cert_domain_name
  most_recent = true
  statuses    = ["ISSUED"]
}
