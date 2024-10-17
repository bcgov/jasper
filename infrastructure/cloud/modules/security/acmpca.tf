# resource "aws_acmpca_certificate_authority" "acmpca_ca" {
#   type                          = "ROOT"
#   usage_mode                    = "GENERAL_PURPOSE"
#   key_storage_security_standard = "FIPS_140_2_LEVEL_3_OR_HIGHER"
#   certificate_authority_configuration {
#     key_algorithm     = "RSA_2048"
#     signing_algorithm = "SHA256WITHRSA"
#     subject {
#       country                      = "CA"
#       organization                 = "bcgov"
#       organizational_unit          = "bccourts"
#       distinguished_name_qualifier = "${var.app_name}-ca-${var.environment}"
#       common_name                  = "${var.app_name}-ca-${var.environment}"
#       state                        = "BC"
#       locality                     = "Vancouver"
#     }
#   }

#   tags = {
#     Name = "${var.app_name}-acmpca-${var.environment}"
#   }
# }

# resource "aws_acmpca_permission" "acmpca_permission" {
#   certificate_authority_arn = aws_acmpca_certificate_authority.acmpca_ca.arn
#   actions                   = ["IssueCertificate", "GetCertificate", "ListPermissions"]
#   principal                 = "acm.amazonaws.com"
# }

# resource "aws_acmpca_certificate" "acmpca_certificate" {
#   certificate_authority_arn   = aws_acmpca_certificate_authority.acmpca_ca.arn
#   certificate_signing_request = aws_acmpca_certificate_authority.acmpca_ca.certificate_signing_request
#   signing_algorithm           = "SHA256WITHRSA"
#   template_arn                = "arn:aws:acm-pca:::template/RootCACertificate/V1"
#   validity {
#     type  = "YEARS"
#     value = 3
#   }
# }

# resource "aws_acmpca_certificate_authority_certificate" "acmpca_cac" {
#   certificate_authority_arn = aws_acmpca_certificate_authority.acmpca_ca.arn

#   certificate       = aws_acmpca_certificate.acmpca_certificate.certificate
#   certificate_chain = aws_acmpca_certificate.acmpca_certificate.certificate_chain
# }
