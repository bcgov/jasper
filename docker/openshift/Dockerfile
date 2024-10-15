FROM alpine:latest
ARG VAULT_VERSION="1.17.6"
ARG APP_ROOT=/usr/local/bin
ARG SRC=./docker/openshift
WORKDIR ${APP_ROOT}

# Install dependencies (jq and AWS CLI)
RUN apk add --no-cache \
    jq \
    aws-cli \
    curl \
    tar

# Download and install the OpenShift CLI (oc)
RUN curl -L https://mirror.openshift.com/pub/openshift-v4/clients/ocp/stable/openshift-client-linux.tar.gz -o /tmp/oc.tar.gz \
    && tar -xvzf /tmp/oc.tar.gz -C /usr/local/bin \
    && chmod +x /usr/local/bin/oc \
    && rm /tmp/oc.tar.gz

# Copy the shell script to the container
COPY ${SRC}/sync_secrets.sh ${APP_ROOT}/sync_secrets.sh

# Ensure that SH script has executable permissions
RUN chmod +x ${APP_ROOT}/sync_secrets.sh

# Command to run the script
CMD [ "./sync_secrets.sh" ]