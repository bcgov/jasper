#!/bin/sh

# Vault details
VAULT_SECRET_ENV="${VAULT_SECRET_ENV}"
LOCAL_SECRET_PATH="${LOCAL_SECRET_PATH}"

aws_secret_format="jasper-X-secret-$VAULT_SECRET_ENV"
secret_keys="\
  aspnet_core \
  auth \
  database \
  file_services_client \
  keycloak \
  location_services_client \
  lookup_services_client \
  misc \
  request \
  splunk \
  user_services_client"

set -x

# AWS Access Keys/IDs has a scheduled rotation and needs to be kept up-to-date in OpenShift.
# https://developer.gov.bc.ca/docs/default/component/public-cloud-techdocs/design-build-and-deploy-an-application/iam-user-service/#setup-automation-to-retrieve-and-use-keys
echo "Checking if AWS keys needs to be updated..."
param_value=$(aws ssm get-parameter --name "/iam_users/openshiftuser${VAULT_SECRET_ENV}_keys" --with-decryption | jq -r '.Parameter.Value')

if [ $? -eq 0 ]; then
  pendingAccessKeyId=$(echo "$param_value" | jq -r '.pending_deletion.AccessKeyID')
  pendingSecretAccessKey=$(echo "$param_value" | jq -r '.pending_deletion.SecretAccessKey')
  currentAccessKeyId=$(echo "$param_value" | jq -r '.current.AccessKeyID')
  currentSecretAccessKey=$(echo "$param_value" | jq -r '.current.SecretAccessKey')

  if [ "$AWS_ACCESS_KEY_ID" = "$pendingAccessKeyId" ] || [ "$AWS_SECRET_ACCESS_KEY" = "$pendingSecretAccessKey" ]; then
    oc create secret generic aws-secret \
      --from-literal=AWS_ACCESS_KEY_ID=$currentAccessKeyId \
      --from-literal=AWS_SECRET_ACCESS_KEY=$currentSecretAccessKey \
      --dry-run=client -o yaml | oc apply -f -
    echo "AWS access key id and secret access key has been updated."
  else
    echo "AWS access key id and secret access key is up-to-date."
  fi
else
  echo "Failed to update openshiftuser key values."
fi

echo "Syncing secrets..."
# Iterate on each key to get the value from Vault and save to AWS secrets manager
for key in $secret_keys; do
  value=$(jq -r ".${VAULT_SECRET_ENV}_$key" "$LOCAL_SECRET_PATH")

  sanitizedKey=$(echo "$key" | sed "s/_/-/g")
  secret_name=$(echo "$aws_secret_format" | sed "s/X/$sanitizedKey/")
  secret_string=$(echo "$value" | jq -c '.')

  echo "Uploading $secret_name"
  aws secretsmanager put-secret-value \
    --secret-id $secret_name \
    --secret-string "$secret_string"
done

if [ $? -eq 0 ]; then
  echo "Secrets synced successfully from Vault to AWS Secrets Manager."
else
  echo "Failed to sync secrets."
fi