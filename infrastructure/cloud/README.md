# JASPER's AWS Infrastructure Setup

This repository includes Terraform scripts for provisioning and managing JASPER's AWS infrastructure. The team has adopted a modularized folder structure to enhance reusability, maintainability, and separation of concerns. The infrastructure-as-code is organized into reusable, encapsulated components known as modules, along with environment-specific configurations. This structure enables consistent and efficient management of infrastructure across various environments, such as development, testing, and production.

## Prerequisites

1. Navigate to [BC Gov's AWS instance](https://login.nimbus.cloud.gov.bc.ca/api).
2. Configure AWS CLI

   ```
   aws configure sso
   ```

3. Follow instructions from CLI.

## Planning and deploying infrastructure via GitHub Actions

For ECR lifecycle changes, complete the [mandatory verification and rollout gates](#mandatory-verification-and-rollout-gates) before merging or running the publishing workflow.

1. Push your branch and open a pull request to `master`. For infrastructure changes, **Build Infra** automatically plans against `lz-dev`.
2. To plan against another environment, open [Actions](https://github.com/bcgov/jasper/actions), select **Build Infra**, click **Run workflow**, and choose your branch and environment. Review the initial and main stack plans in the run logs.
3. Merging an infrastructure change to `master` triggers **Publish Infra** for `lz-dev`. To deploy to another environment, select **Publish Infra** in Actions, click **Run workflow**, and choose the branch and environment after reviewing its plan.

## ECR image retention

The shared `modules/initial` configuration applies the same policies in `lz-dev`, `lz-test`, and `lz-prod`:

| Repository | Tag patterns | Expiration |
| --- | --- | --- |
| Application | `api-*`, `web-*` | Beyond the latest 10 images per pattern |
| Lambda | `auth.authorizer-*`, `auth.rotate-key-*`, `proxy.get-assigned-cases-request-*`, `proxy.proxy-request-*` | Beyond the latest 10 images per pattern |
| Both | Untagged | Older than 7 days |

Each component has an independent rule with one pattern in `tagPatternList`, `countType = "imageCountMoreThan"`, `countNumber = 10`, and `action.type = "expire"`. Tagged rules omit `countUnit` and start at priority 2 in sorted pattern order. The untagged rule keeps priority 1 and uses `sinceImagePushed` with `countUnit = "days"` and `countNumber = 7`.

ECR orders matching images by push time, independent of deployment time or last use. Each rule retains the 10 most recently pushed matching images and marks older images beyond that count for expiration. Infrequently deployed components with 10 or fewer matches keep those images regardless of age. Commit and formal release tags are covered. Images tagged only as `dummy-image` or with only unmatched tags are unaffected. Images that lose all tags become subject to the seven-day rule. Component patterns belong in the caller's required `tagged_image_patterns` set, which rejects empty or invalid patterns.

Count-based retention does not guarantee that deployed or rollback images survive. Repeated failed deployments can push enough newer images to displace the image still serving traffic. Rolling back to an older image can also leave it outside the retained 10, since redeploying an existing image does not necessarily update its ECR push time. Ten images do not guarantee ten successful deployments or ten complete formal releases. You may be able to recover an expired image from GHCR if it is still available there. Restoring an earlier lifecycle policy cannot recover deleted images.

### Review the GitHub Actions plan and preview ECR expiration

For each environment, review **Terraform Plan (Initial Stack)** in the **Build Infra** run for the branch you intend to deploy. For a lifecycle-only change, confirm that the application and Lambda lifecycle policies are the only resources changing.

Use the AWS CLI and `jq` with credentials for that environment. Fill in the names of the two repositories from the plan.

```bash
set -euo pipefail
environment=lz-dev # Repeat for lz-test and lz-prod.
export AWS_PROFILE='<profile for this environment>'
export AWS_REGION=ca-central-1
export AWS_PAGER=''
aws sts get-caller-identity
app_repository='<application ECR repository name>'
lambda_repository='<Lambda ECR repository name>'
umask 077
review_dir="$(mktemp -d "${TMPDIR:-/tmp}/jasper-ecr-${environment}.XXXXXX")"
```

**Build Infra** prints a plan diff in its logs. Save the full proposed policy for each repository as `app-policy.json` and `lambda-policy.json` in `$review_dir`. If the plan hides unchanged fields, use `aws ecr get-lifecycle-policy` to retrieve the current policy and apply the changes shown in the plan to a local copy. Block rollout if you cannot establish the exact proposed policies from the plan and current configuration.

The commands below verify the saved policies and preview expiration without installing policies or deleting images. Keep AWS CLI pagination enabled and coordinate with other operators if a preview is already running.

```bash
for repository_type in app lambda; do
  case "$repository_type" in
    app)
      repository="$app_repository"
      patterns='["api-*","web-*"]'
      ;;
    lambda)
      repository="$lambda_repository"
      patterns='["auth.authorizer-*","auth.rotate-key-*","proxy.get-assigned-cases-request-*","proxy.proxy-request-*"]'
      ;;
  esac
  jq -e --argjson patterns "$patterns" '
    (.rules | map(.rulePriority)) == [range(1; (($patterns | length) + 2))]
    and (.rules | all(.action.type == "expire"))
    and .rules[0].selection == {
      tagStatus: "untagged", countType: "sinceImagePushed", countUnit: "days", countNumber: 7
    }
    and (.rules[1:] | map(.selection)) == ($patterns | map({
      tagStatus: "tagged", tagPatternList: [.], countType: "imageCountMoreThan", countNumber: 10
    }))
  ' "$review_dir/$repository_type-policy.json" > /dev/null

  aws ecr start-lifecycle-policy-preview --repository-name "$repository" \
    --lifecycle-policy-text "file://$review_dir/$repository_type-policy.json"

  while true; do
    aws ecr get-lifecycle-policy-preview --repository-name "$repository" --output json \
      > "$review_dir/$repository_type-preview.json"
    status="$(jq -r '.status' "$review_dir/$repository_type-preview.json")"
    case "$status" in
      COMPLETE) break ;;
      IN_PROGRESS) sleep 5 ;;
      *) printf 'Preview failed: %s\n' "$status" >&2; exit 1 ;;
    esac
  done

  jq -e --rawfile policy "$review_dir/$repository_type-policy.json" '
    (.lifecyclePolicyText | fromjson) == ($policy | fromjson)
  ' "$review_dir/$repository_type-preview.json" > /dev/null
  jq -r '.previewResults[] | select(.action.type == "EXPIRE")
    | [.imageDigest, ((.imageTags // []) | join(","))] | @tsv' \
    "$review_dir/$repository_type-preview.json" > "$review_dir/$repository_type-candidates.tsv"
  aws ecr describe-images --repository-name "$repository" --output json \
    > "$review_dir/$repository_type-inventory.json"
done
```

Run fresh previews of both proposed policies before deploying to each environment.

### Mandatory verification and rollout gates

**High risk:** ECR does not protect images referenced by ECS or Lambda. Expiration can affect restarts, authorization, key rotation, and rollback; restoring a policy cannot restore deleted images. Before applying in each environment:

1. Verify the extracted policies against the table, sorted priorities, and seven-day untagged rule. Compare candidate digests with inventory push times for each pattern. Confirm the latest 10 matching images are retained, or all matching images when there are fewer than 10. Check that commit and release tags match their component rule and that images tagged only as `dummy-image` or with only unmatched tags are absent from the candidates.
2. Compare every candidate digest against configured ECS task definition images, active deployments, running container digests, current Lambda resolved digests, required published versions (including alias routing), infrastructure references, SSM image parameters, and agreed rollback images/historical task definitions. Resolve tags with `aws ecr describe-images`; include manifest-list and platform digests where relevant.
3. Read actual AWS references using `aws ecs describe-services`, `describe-tasks`, and `describe-task-definition`; `aws lambda get-function` (also with `--qualifier` for required versions), `list-versions-by-function`, and `list-aliases`; and `aws ssm get-parameter` for `/images/<app>-{api,web}-image-param-<environment>`. Terraform ignores Lambda `image_uri` changes, so the plan alone is insufficient.
4. **Block deployment if any required image would expire or any reference is unresolved.** Obtain rollout confirmation after the comparison. Repeat plans, previews, and reference checks if time, inventory, or references change; verify the policy being applied matches the preview, including any initial-stack targeted plan.

Complete development checks before merging: `publish-infra.yml` applies on pushes to `master`, defaulting to `lz-dev`. Repeat the gates for test and production, then verify required images and normal ECS/Lambda operation after deployment.

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
