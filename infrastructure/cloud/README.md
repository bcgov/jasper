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
