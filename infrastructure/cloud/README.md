# JASPER's AWS Infrastructure Setup

This repository includes Terraform scripts for provisioning and managing JASPER's AWS infrastructure. The team has adopted a modularized folder structure to enhance reusability, maintainability, and separation of concerns. The infrastructure-as-code is organized into reusable, encapsulated components known as modules, along with environment-specific configurations. This structure enables consistent and efficient management of infrastructure across various environments, such as development, testing, and production.

## Prerequisites

1. Navigate to [BC Gov's AWS instance](https://login.nimbus.cloud.gov.bc.ca/api).
2. Configure AWS CLI

   ```
   aws configure sso
   ```

3. Follow instructions from CLI.

## Running Terraform Scripts Locally

1. Navigate to the desired environment (`/dev` or `/test`) where you want the Terraform scripts to be executed.
2. Initialize the working directory.

   ```
   terraform init -backend-config=backend.tfvars
   ```

3. Preview the changes that Terraform plans to deploy.

   ```
   terraform plan -var-file="./<environment>.tfvars"
   ```

4. If everything looks good, execute the actions propsed Terraform plan.

   ```
   terraform apply -var-file="./<environment>.tfvars"
   ```

## Deploying Terraform changes via Github Actions

1. Commit and push your working branch to Github.
2. Navigate to [Actions](https://github.com/bcgov/jasper/actions) tab.
3. Select the desired workflow (Deploy AWS Infra to `<environment>`).
4. Click `Run workflow` dropdown.
5. Select working branch
6. Click `Run workflow` button.

## Failure email environment

The API task definition sets `DEPLOYMENT_ENVIRONMENT` from the Terraform deployment name. The `lz-dev`, `lz-test`, and `lz-prod` deployments produce `[DEV]`, `[TEST]`, and `[PROD]` subjects. API instances with a missing or unrecognized value use `[UNKNOWN]`.

Apply the infrastructure change in each environment so the API task definition includes the setting. The API adds the prefix after rendering `Job Failure` and `Order Submission Failed` subjects. Existing database templates and configured subject text require no manual update. New databases receive the templates through the existing seeder.

## Connecting to an ECS container

The DevContainer for the project includes the tools needed to shell into an ECS container. This includes the `aws` cli and the AWS Session Manager Plugin.

Following is and example of the steps needed to shell into a container.

1. Login to the [BC Gov's AWS instance](https://login.nimbus.cloud.gov.bc.ca/api) portal.
2. Get the credentials for the desired environment and paste them into a DevContainer terminal session.
3. Enable the Execute Command on the continer(s).
   ```
   aws ecs update-service --cluster jasper-app-cluster-dev --service jasper-api-ecs-service-dev --enable-execute-command --force-new-deployment
   ```
4. Once the new task rolls out, verify the execute command is enabled.
   ```
   aws ecs describe-tasks \
       --cluster jasper-app-cluster-dev  \
       --tasks arn:aws:ecs:ca-central-1:381491824201:task/jasper-app-cluster-dev/5f8eeabe7e1943c5ba93c3ae55d48933
   ```
   Look for the following in the output:
   ```
       ...
       "enableExecuteCommand": true,
       ...
   ```
5. Shell into the container
   ```
   aws ecs execute-command --cluster jasper-app-cluster-dev \
       --task arn:aws:ecs:ca-central-1:381491824201:task/jasper-app-cluster-dev/5f8eeabe7e1943c5ba93c3ae55d48933 \
       --interactive \
       --command "/bin/sh"
   ```
6. When complete `exit` the container.
7. Disable the Execute Command on the continer(s).
   ```
   aws ecs update-service --cluster jasper-app-cluster-dev --service jasper-api-ecs-service-dev --disable-execute-command --force-new-deployment
   ```
