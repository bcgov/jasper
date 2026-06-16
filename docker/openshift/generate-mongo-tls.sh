#!/bin/sh
set -eu

umask 077

log() {
  echo "[generate-mongo-tls] $*"
}

fail() {
  echo "[generate-mongo-tls] ERROR: $*" >&2
  exit 1
}

required() {
  name="$1"
  eval "value=\${$name:-}"
  if [ -z "$value" ]; then
    fail "Missing required environment variable: $name"
  fi
}

required CERT_SUBJECT_ALT_NAME
required OPENSHIFT_SECRET_NAME
required OPENSHIFT_SECRET_NAMESPACE
required AWS_SECRET_ID
required MONGO_STATEFULSET_SELECTOR
required CA_CERT_PATH
required CA_KEY_PATH

CERT_DAYS="${CERT_DAYS:-14}"
KEY_SIZE="${KEY_SIZE:-4096}"
CERT_COMMON_NAME="${CERT_COMMON_NAME:-cloudpirates-mongodb}"
ROLLOUT_TIMEOUT="${ROLLOUT_TIMEOUT:-300s}"

# CERT_SUBJECT_ALT_NAME must be a valid openssl subjectAltName value string.
# e.g. "IP:10.99.10.9,DNS:my-host.svc.cluster.local,DNS:localhost"

GENERATED_CERT_KEY="${GENERATED_CERT_KEY:-tls.crt}"
GENERATED_PRIVATE_KEY_KEY="${GENERATED_PRIVATE_KEY_KEY:-tls.key}"
GENERATED_PEM_KEY="${GENERATED_PEM_KEY:-tls.pem}"
CA_BUNDLE_KEY="${CA_BUNDLE_KEY:-ca.crt}"

AWS_FORCE_PUT="${AWS_FORCE_PUT:-false}"

WORKDIR="$(mktemp -d)"

SERVER_KEY="$WORKDIR/tls.key"
SERVER_CSR="$WORKDIR/tls.csr"
SERVER_CRT="$WORKDIR/tls.crt"
SERVER_PEM="$WORKDIR/tls.pem"
SERVER_EXT="$WORKDIR/server-ext.cnf"

AWS_GET_JSON="$WORKDIR/aws-secret-current.json"
AWS_BACKUP="$WORKDIR/aws-secret-backup.pem"
K8S_BACKUP="$WORKDIR/k8s-secret-backup.json"

AWS_MUTATED="false"
K8S_MUTATED="false"
K8S_SECRET_EXISTED="false"

cleanup() {
  rm -rf "$WORKDIR"
}

get_statefulsets() {
  kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" get statefulset \
    -l "$MONGO_STATEFULSET_SELECTOR" \
    -o jsonpath='{range .items[*]}{.metadata.name}{"\n"}{end}'
}

restart_mongo_statefulsets() {
  STATEFULSETS="$(get_statefulsets)"

  if [ -z "$STATEFULSETS" ]; then
    fail "No StatefulSets found for selector: $MONGO_STATEFULSET_SELECTOR"
  fi

  for STATEFULSET in $STATEFULSETS; do
    log "Restarting StatefulSet: ${OPENSHIFT_SECRET_NAMESPACE}/${STATEFULSET}"
    kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" rollout restart statefulset "$STATEFULSET"
  done

  for STATEFULSET in $STATEFULSETS; do
    log "Waiting for StatefulSet rollout: ${OPENSHIFT_SECRET_NAMESPACE}/${STATEFULSET}"
    kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" rollout status statefulset "$STATEFULSET" \
      --timeout="$ROLLOUT_TIMEOUT"
  done
}

restore_k8s_secret() {
  if [ "$K8S_MUTATED" != "true" ]; then
    return 0
  fi

  if [ "$K8S_SECRET_EXISTED" = "true" ] && [ -f "$K8S_BACKUP" ]; then
    log "Rolling back OpenShift secret: ${OPENSHIFT_SECRET_NAMESPACE}/${OPENSHIFT_SECRET_NAME}"
    kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" apply -f "$K8S_BACKUP" >/dev/null || true
  else
    log "Deleting newly-created OpenShift secret: ${OPENSHIFT_SECRET_NAMESPACE}/${OPENSHIFT_SECRET_NAME}"
    kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" delete secret "$OPENSHIFT_SECRET_NAME" \
      --ignore-not-found=true >/dev/null || true
  fi
}

restore_aws_secret() {
  if [ "$AWS_MUTATED" != "true" ]; then
    return 0
  fi

  if [ -f "$AWS_BACKUP" ]; then
    log "Rolling back AWS secret: $AWS_SECRET_ID"
    aws secretsmanager put-secret-value \
      --secret-id "$AWS_SECRET_ID" \
      --secret-string "file://$AWS_BACKUP" >/dev/null || true
  fi
}

on_exit() {
  status="$1"

  if [ "$status" -eq 0 ]; then
    cleanup
    exit 0
  fi

  set +e

  log "Certificate rotation failed. Attempting rollback."

  restore_k8s_secret

  if [ "$K8S_MUTATED" = "true" ]; then
    log "Restarting MongoDB after OpenShift secret rollback."
    restart_mongo_statefulsets || true
  fi

  restore_aws_secret

  cleanup

  log "Rollback attempt completed. Original failure status: $status"
  exit "$status"
}

trap 'status=$?; on_exit "$status"' EXIT

command -v openssl >/dev/null 2>&1 || fail "openssl is required"
command -v jq >/dev/null 2>&1 || fail "jq is required"
command -v aws >/dev/null 2>&1 || fail "aws CLI is required"
command -v kubectl >/dev/null 2>&1 || fail "kubectl is required"

[ -f "$CA_CERT_PATH" ] || fail "CA cert file not found: $CA_CERT_PATH"
[ -f "$CA_KEY_PATH" ] || fail "CA key file not found: $CA_KEY_PATH"

log "Validating AWS secret exists: $AWS_SECRET_ID"
aws secretsmanager describe-secret --secret-id "$AWS_SECRET_ID" >/dev/null

log "Backing up current AWS secret value."
aws secretsmanager get-secret-value \
  --secret-id "$AWS_SECRET_ID" \
  --output json > "$AWS_GET_JSON"

jq -e 'has("SecretString") and (.SecretString != null)' "$AWS_GET_JSON" >/dev/null \
  || fail "AWS secret must contain SecretString, not SecretBinary."

jq -r '.SecretString' "$AWS_GET_JSON" > "$AWS_BACKUP"

log "Checking existing OpenShift secret: ${OPENSHIFT_SECRET_NAMESPACE}/${OPENSHIFT_SECRET_NAME}"
if kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" get secret "$OPENSHIFT_SECRET_NAME" >/dev/null 2>&1; then
  K8S_SECRET_EXISTED="true"

  kubectl -n "$OPENSHIFT_SECRET_NAMESPACE" get secret "$OPENSHIFT_SECRET_NAME" -o json \
    | jq '
        del(
          .metadata.uid,
          .metadata.resourceVersion,
          .metadata.creationTimestamp,
          .metadata.managedFields,
          .metadata.ownerReferences,
          .metadata.annotations."kubectl.kubernetes.io/last-applied-configuration"
        )
      ' > "$K8S_BACKUP"
fi

log "Generating MongoDB server private key."
openssl genrsa -out "$SERVER_KEY" "$KEY_SIZE"

log "Generating MongoDB server certificate signing request."
openssl req \
  -new \
  -key "$SERVER_KEY" \
  -out "$SERVER_CSR" \
  -subj "/CN=${CERT_COMMON_NAME}"

cat > "$SERVER_EXT" <<EOF
subjectAltName = ${CERT_SUBJECT_ALT_NAME}
basicConstraints = critical,CA:FALSE
keyUsage = critical,digitalSignature,keyEncipherment
extendedKeyUsage = serverAuth
subjectKeyIdentifier = hash
authorityKeyIdentifier = keyid,issuer
EOF

log "Signing MongoDB server certificate with mounted private CA."
SERIAL_HEX="$(openssl rand -hex 16)"

openssl x509 \
  -req \
  -in "$SERVER_CSR" \
  -CA "$CA_CERT_PATH" \
  -CAkey "$CA_KEY_PATH" \
  -set_serial "0x${SERIAL_HEX}" \
  -out "$SERVER_CRT" \
  -days "$CERT_DAYS" \
  -sha256 \
  -extfile "$SERVER_EXT"

cat "$SERVER_KEY" "$SERVER_CRT" > "$SERVER_PEM"

log "Validating generated certificate."
openssl verify -CAfile "$CA_CERT_PATH" "$SERVER_CRT" >/dev/null

log "Ensuring AWS secret contains current CA PEM."
if [ "$AWS_FORCE_PUT" = "true" ] || ! cmp -s "$AWS_BACKUP" "$CA_CERT_PATH"; then
  aws secretsmanager put-secret-value \
    --secret-id "$AWS_SECRET_ID" \
    --secret-string "file://$CA_CERT_PATH" >/dev/null
  AWS_MUTATED="true"
  log "AWS secret updated: $AWS_SECRET_ID"
else
  log "AWS secret already matches CA PEM; skipping AWS put-secret-value."
fi

log "Applying OpenShift TLS secret: ${OPENSHIFT_SECRET_NAMESPACE}/${OPENSHIFT_SECRET_NAME}"
kubectl create secret generic "$OPENSHIFT_SECRET_NAME" \
  -n "$OPENSHIFT_SECRET_NAMESPACE" \
  --from-file="${CA_BUNDLE_KEY}=${CA_CERT_PATH}" \
  --from-file="${GENERATED_CERT_KEY}=${SERVER_CRT}" \
  --from-file="${GENERATED_PRIVATE_KEY_KEY}=${SERVER_KEY}" \
  --from-file="${GENERATED_PEM_KEY}=${SERVER_PEM}" \
  --dry-run=client \
  -o yaml \
  | kubectl apply -f - >/dev/null

K8S_MUTATED="true"

restart_mongo_statefulsets

log "MongoDB TLS rotation completed successfully."