#!/bin/sh

# Vault details
VAULT_SECRET_ENV="${VAULT_SECRET_ENV}"
LOCAL_SECRET_PATH="${LOCAL_SECRET_PATH}"
AWS_SECRET_PREFIX="${AWS_SECRET_PREFIX}"

aws_secret_format="external/jasper-X-secret-$AWS_SECRET_PREFIX$VAULT_SECRET_ENV"
secret_keys="\
  aspnet_core \
  auth \
  azure \
  dars \
  database \
  file_services_client \
  jobs \
  keycloak \
  location_services_client \
  lookup_services_client \
  misc \
  mtls_cert \
  nutrient \
  pcss \
  request \
  reserved_judgements \
  splunk \
  user_services_client \
  keycloak_td \
  smb"

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