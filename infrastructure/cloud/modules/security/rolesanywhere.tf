# resource "aws_rolesanywhere_trust_anchor" "ra_ta" {
#   name    = "${var.app_name}-ra-ta-${var.environment}"
#   enabled = true


#   source {
#     source_data {
#       acm_pca_arn = aws_acmpca_certificate_authority.acmpca_ca.arn
#     }
#     source_type = "AWS_ACM_PCA"
#   }

#   depends_on = [aws_acmpca_certificate_authority.acmpca_ca]

#   tags = {
#     Name = "${var.app_name}-ra-ta-${var.environment}"
#   }
# }

# resource "aws_rolesanywhere_profile" "ra_profile" {
#   name                        = "${var.app_name}-ra-profile-${var.environment}"
#   enabled                     = true
#   require_instance_properties = false
#   role_arns                   = [aws_iam_role.rolesanywhere_role.arn]

#   tags = {
#     Name = "${var.app_name}-ra-profile-${var.environment}"
#   }
# }
