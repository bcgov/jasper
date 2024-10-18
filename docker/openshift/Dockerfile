FROM alpine:latest
ARG VAULT_VERSION="1.17.6"
ARG APP_ROOT=/usr/local/bin
ARG SRC=./docker/openshift

# Install dependencies
RUN apk add --no-cache \
    jq \
    aws-cli \
    curl \
    tar \
    bash \
    libc6-compat

# Download and install the OpenShift CLI (oc)
RUN curl -L https://mirror.openshift.com/pub/openshift-v4/clients/ocp/stable/openshift-client-linux.tar.gz -o /tmp/openshift-client-linux.tar.gz && \
    tar -zxvf /tmp/openshift-client-linux.tar.gz -C /usr/local/bin && \
    rm /tmp/openshift-client-linux.tar.gz

WORKDIR ${APP_ROOT}

# Copy the shell script to the container
COPY ${SRC}/sync_secrets.sh ${APP_ROOT}/sync_secrets.sh

# Ensure that oc and SH script has executable permissions
RUN chmod +x ${APP_ROOT}/sync_secrets.sh oc

RUN oc version --client

# Command to run the script
CMD [ "./sync_secrets.sh" ]