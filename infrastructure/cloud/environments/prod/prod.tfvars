region              = "ca-central-1"
test_s3_bucket_name = "jasper-test-s3-bucket-test"
web_subnet_names    = ["Web_Prod_aza_net", "Web_Prod_azb_net"]
app_subnet_names    = ["App_Prod_aza_net", "App_Prod_azb_net"]
data_subnet_names   = ["Data_Prod_aza_net", "Data_Prod_azb_net"]
openshift_iam_user  = "openshiftuserprod"
iam_user_table_name = "BCGOV_IAM_USER_TABLE"
lb_name             = "default"
rds_db_ca_cert      = "rds-ca-rsa2048-g1"
cert_domain_name    = "*.example.ca"
