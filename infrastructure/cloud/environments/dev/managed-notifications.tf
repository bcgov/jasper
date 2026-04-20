module "managed_notifications" {
  source = "../../modules/ManagedNotifications"

  account_id               = data.aws_caller_identity.current.account_id
  environment              = var.environment
  notification_hub_region  = var.region
  email_addresses          = tolist(toset(nonsensitive(module.secrets_manager.managed_notifications_email_addresses)))
  managed_notification_sub_categories = ["Security", "Operations", "Issue"]
}
