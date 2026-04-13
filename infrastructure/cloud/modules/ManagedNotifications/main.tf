locals {
  aws_health_managed_notification_configuration_arns = toset([
    for sub_category in var.managed_notification_sub_categories :
    "arn:aws:notifications::${var.account_id}:managed-notification-configuration/category/AWS-Health/sub-category/${sub_category}"
  ])
}

resource "aws_notifications_notification_hub" "aws_health" {
  notification_hub_region = var.notification_hub_region
}

resource "aws_notificationscontacts_email_contact" "aws_health" {
  for_each      = toset(var.email_addresses)
  name          = "health-${var.environment}-${substr(sha1(each.value), 0, 12)}"
  email_address = each.value
}

resource "aws_notifications_managed_notification_additional_channel_association" "aws_health_email_contacts" {
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
