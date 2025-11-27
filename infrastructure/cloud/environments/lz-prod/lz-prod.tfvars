region                    = "ca-central-1"
test_s3_bucket_name       = "jasper-test-s3-bucket-test"
web_subnet_names          = ["Prod-Web-MainTgwAttach-B", "Prod-Web-MainTgwAttach-A"]
app_subnet_names          = ["Prod-App-A", "Prod-App-B"]
data_subnet_names         = ["Prod-Data-A", "Prod-Data-B"]
openshift_iam_user        = "openshiftuserprod"
iam_user_table_name       = "BCGOV-LZA-IAM-USER-TABLE"
lb_name                   = "default"
rds_db_ca_cert            = "rds-ca-rsa2048-g1"
cert_domain_name          = "internal.stratus.cloud.gov.bc.ca"
delete_protection_enabled = true
mongo_node_count          = 1
mongo_instance_type       = "db.t3.medium"
mongousername             = "adminmongo"
web_ecs_config = {
  min_capacity = 3
  max_capacity = 6
  cpu          = 256
  memory_size  = 512
}
api_ecs_config = {
  min_capacity = 3
  max_capacity = 6
  cpu          = 1024
  memory_size  = 2048
}
alarm_config = {
  cpu_threshold             = 70
  memory_threshold          = 80
  evaluation_periods        = 3
  period                    = 60
  task_count_low_threshold  = 1
  task_count_high_threshold = 3
  task_evaluation_periods   = 1
  task_period               = 30
}
efs_config = {
  mount_path = "/mnt/efs"
  files_dir  = "files"
}
get_assigned_cases_lambda_timeout = 10 // minutes
lambda_long_timeout               = 15 // minutes
lambda_retry_attempts             = 3
lza_log_archive_account_id        = "897722703828"
