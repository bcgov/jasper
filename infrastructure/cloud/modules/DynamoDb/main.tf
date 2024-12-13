#
# Openshift
#
# https://developer.gov.bc.ca/docs/default/component/public-cloud-techdocs/design-build-and-deploy-an-application/iam-user-service/
# Step 1: Add opeshiftuser if not exist
data "aws_dynamodb_table" "iam_user_table" {
  name = var.iam_user_table_name
}

resource "null_resource" "check_and_insert_openshiftuser_record" {
  triggers = {
    always_run = timestamp()
  }

  provisioner "local-exec" {
    command = <<EOT
      # Check if the 'openshiftuser' record exists
      EXISTS=$(aws dynamodb get-item --table-name ${var.iam_user_table_name} --key '{"UserName": {"S": "${var.openshift_iam_user}"}}' --query 'Item')

      # Check for errors from the previous command
      if [ $? -ne 0 ]; then
        echo "Failed to check for record: $EXISTS"
        exit 1
      fi

      # If the record does not exist, insert it
      if [ -z "$EXISTS" ] || [ "$EXISTS" = "null" ]; then
        echo "Record does not exist. Inserting..."
        aws dynamodb put-item --table-name ${var.iam_user_table_name} --item '{"UserName": {"S": "${var.openshift_iam_user}"}}'
      else
        echo "Record already exists. No action taken."
      fi
    EOT
  }
}

# Step 2: Wait for openshift user to become available
resource "null_resource" "wait_for_openshift_iam_user" {
  depends_on = [null_resource.check_and_insert_openshiftuser_record]
  provisioner "local-exec" {
    command = <<EOT
    for i in {1..10}; do
      aws iam get-user --user-name ${var.openshift_iam_user} && exit 0 || echo "Waiting for user..." && sleep 10
    done
    exit 1
    EOT
  }
}
