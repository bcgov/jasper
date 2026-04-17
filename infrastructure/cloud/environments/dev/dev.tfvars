region                    = "ca-central-1"
test_s3_bucket_name       = "jasper-test-s3-bucket-dev"
web_subnet_names          = ["Web_Dev_aza_net", "Web_Dev_azb_net"]
app_subnet_names          = ["App_Dev_aza_net", "App_Dev_azb_net"]
data_subnet_names         = ["Data_Dev_aza_net", "Data_Dev_azb_net"]
openshift_iam_user        = "openshiftuserdev"
iam_user_table_name       = "BCGOV_IAM_USER_TABLE"
lb_name                   = "default"
rds_db_ca_cert            = "rds-ca-rsa2048-g1"
cert_domain_name          = "*.example.ca"
delete_protection_enabled = false
mongo_node_count          = 1
mongo_instance_type       = "db.t3.medium"
mongousername             = "adminmongo"
web_ecs_config = {
  min_capacity = 1
  max_capacity = 1
  cpu          = 256
  memory_size  = 512
}
api_ecs_config = {
  min_capacity = 1
  max_capacity = 1
  cpu          = 1024
  memory_size  = 3072 # Increased from 2048 to accommodate ClamAV sidecar (~512MB)
}
alarm_config = {
  cpu_threshold             = 70
  memory_threshold          = 80
  evaluation_periods        = 3
  period                    = 60
  task_count_low_threshold  = 1
  task_count_high_threshold = 1
  task_evaluation_periods   = 1
  task_period               = 30
}
efs_config = {
  mount_path = "/mnt/efs"
  files_dir  = "files"
}
get_assigned_cases_lambda_timeout = 5 // minutes
lambda_long_timeout               = 5 // minutes
lambda_retry_attempts             = 0
clamav_config = {
  image              = "clamav/clamav@sha256:c6eb128c7bd57bb0c533491198753a2130b471c517605ad8d289174a56c450a8" # v1.5.2
  host               = "localhost" // ClamAV will run as a sidecar container, so it can be accessed via localhost
  port               = 3310
  memory_reservation = 512
  stream_max_length  = "100M"
}
