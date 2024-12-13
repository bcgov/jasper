#!/bin/sh
ENVIRONMENT="${ENVIRONMENT}"

# AWS Access Keys/IDs has a scheduled rotation and needs to be kept up-to-date in OpenShift.
# https://developer.gov.bc.ca/docs/default/component/public-cloud-techdocs/design-build-and-deploy-an-application/iam-user-service/#setup-automation-to-retrieve-and-use-keys
echo "Checking if AWS keys needs to be updated..."
param_value=$(aws ssm get-parameter --name "/iam_users/openshiftuser${ENVIRONMENT}_keys" --with-decryption | jq -r '.Parameter.Value')

if [ $? -eq 0 ]; then
  pendingAccessKeyId=$(echo "$param_value" | jq -r '.pending_deletion.AccessKeyID')
  pendingSecretAccessKey=$(echo "$param_value" | jq -r '.pending_deletion.SecretAccessKey')
  currentAccessKeyId=$(echo "$param_value" | jq -r '.current.AccessKeyID')
  currentSecretAccessKey=$(echo "$param_value" | jq -r '.current.SecretAccessKey')

  if [ "$AWS_ACCESS_KEY_ID" = "$pendingAccessKeyId" ] || [ "$AWS_SECRET_ACCESS_KEY" = "$pendingSecretAccessKey" ]; then
    echo "Updating AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY for aws-secret-${ENVIRONMENT} secret..."
    
    oc create secret generic aws-secret-${ENVIRONMENT} \
      --from-literal=AWS_ACCESS_KEY_ID=$currentAccessKeyId \
      --from-literal=AWS_SECRET_ACCESS_KEY=$currentSecretAccessKey \
      --dry-run=client -o yaml | oc apply -f -

    echo "Done."    
  else
    echo "Credentials are up-to-date."
  fi
else
  echo "Failed to query credentials from AWS."
fi