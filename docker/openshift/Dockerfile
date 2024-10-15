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

# Install Openshift CLI
RUN apk add --no-cache --virtual oc-build curl tar \
    && curl -sfL https://mirror.openshift.com/pub/openshift-v4/clients/ocp/stable-4.10/openshift-client-linux.tar.gz | tar -zxvf - -C ${APP_ROOT} \
    && apk del oc-build \
    && rm -rf /var/cache/apk/*

# Copy the shell script to the container
COPY ${SRC}/sync_secrets.sh ${APP_ROOT}/sync_secrets.sh

# Ensure that SH script has executable permissions
RUN chmod +x ${APP_ROOT}/sync_secrets.sh

# Command to run the script
CMD [ "./sync_secrets.sh" ]