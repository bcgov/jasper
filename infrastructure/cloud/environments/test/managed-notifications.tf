locals {
  aws_health_managed_notification_configuration_arns = toset([
    "arn:aws:notifications::${data.aws_caller_identity.current.account_id}:managed-notification-configuration/category/AWS-Health/sub-category/Security",
    "arn:aws:notifications::${data.aws_caller_identity.current.account_id}:managed-notification-configuration/category/AWS-Health/sub-category/Health-Operations",
    "arn:aws:notifications::${data.aws_caller_identity.current.account_id}:managed-notification-configuration/category/AWS-Health/sub-category/Account-Specific-Issues"
  ])
}

provider "aws" {
  alias  = "notifications_hub"
  region = var.region
}

resource "aws_notifications_notification_hub" "aws_health" {
  provider                = aws.notifications_hub
  notification_hub_region = var.region
}

resource "aws_notificationscontacts_email_contact" "aws_health" {
  provider      = aws.notifications_hub
  for_each      = toset(module.secrets_manager.managed_notifications_email_addresses)
  name          = "health-${var.environment}-${substr(sha1(each.value), 0, 12)}"
  email_address = each.value
}

resource "aws_notifications_managed_notification_additional_channel_association" "aws_health_email_contacts" {
  provider = aws.notifications_hub
  for_each = {
    for pair in setproduct(local.aws_health_managed_notification_configuration_arns, keys(aws_notificationscontacts_email_contact.aws_health)) :
    "${pair[0]}::${pair[1]}" => {
      managed_notification_arn = pair[0]
      email_contact_key        = pair[1]
    }
  }

  managed_notification_arn = each.value.managed_notification_arn
  channel_arn              = aws_notificationscontacts_email_contact.aws_health[each.value.email_contact_key].arn

  depends_on = [aws_notifications_notification_hub.aws_health]
}
