#!/bin/sh

# Vault details
VAULT_SECRET_ENV="${VAULT_SECRET_ENV}"
LOCAL_SECRET_PATH="${LOCAL_SECRET_PATH}"

export HTTP_PROXY=http://swpxkam.gov.bc.ca:8080
export HTTPS_PROXY=http://swpxkam.gov.bc.ca:8080
export NO_PROXY=.cluster.local,.svc,10.91.0.0/16,172.30.0.0/16,127.0.0.1,localhost,.gov.bc.ca

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

echo "Show all s3 buckets"
aws s3 ls

# TODO: Add access keys rotation

# Iterate on each key to get the value from Vault and save in AWS secrets manager
for key in $secret_keys; do
  value=$(jq -r ".${VAULT_SECRET_ENV}_$key" "$LOCAL_SECRET_PATH")

  echo "=========================="
  echo "KEY: $key"
  echo "VALUE: $value"

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