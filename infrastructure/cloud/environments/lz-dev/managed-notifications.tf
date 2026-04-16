moved {
  from = aws_notifications_notification_hub.aws_health
  to   = module.managed_notifications.aws_notifications_notification_hub.aws_health
}

moved {
  from = aws_notificationscontacts_email_contact.aws_health
  to   = module.managed_notifications.aws_notificationscontacts_email_contact.aws_health
}

moved {
  from = aws_notifications_managed_notification_additional_channel_association.aws_health_email_contacts
  to   = module.managed_notifications.aws_notifications_managed_notification_additional_channel_association.aws_health_email_contacts
}

module "managed_notifications" {
  source = "../../modules/ManagedNotifications"

  providers = {
    aws = aws.notifications_hub
  }

  account_id                          = data.aws_caller_identity.current.account_id
  environment                         = var.environment
  notification_hub_region             = var.region
  email_addresses                     = tolist(toset(nonsensitive(module.secrets_manager.managed_notifications_email_addresses)))
  managed_notification_sub_categories = ["Security", "Operations", "Issue"]
}
