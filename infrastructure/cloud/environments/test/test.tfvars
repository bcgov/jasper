region                        = "ca-central-1"
test_s3_bucket_name           = "jasper-test-s3-bucket-test"
web_subnet_names              = ["Web_Test_aza_net", "Web_Test_azb_net"]
app_subnet_names              = ["App_Test_aza_net", "App_Test_azb_net"]
data_subnet_names             = ["Data_Test_aza_net", "Data_Test_azb_net"]
openshift_iam_user            = "openshiftusertest"
iam_user_table_name           = "BCGOV_IAM_USER_TABLE"
lb_name                       = "default"
rds_db_ca_cert                = "rds-ca-rsa2048-g1"
cert_domain_name              = "*.example.ca"
delete_protection_enabled     = false
mongo_node_count              = 1
mongo_instance_type           = "db.t3.medium"
mongousername                 = "adminmongo"
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
  memory_size  = 2048
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
