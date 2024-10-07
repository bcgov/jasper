FROM alpine:latest
ARG VAULT_VERSION="1.17.6"
ARG APP_ROOT=/usr/local/bin
WORKDIR ${APP_ROOT}

# Install dependencies (jq and AWS CLI)
RUN apk add --no-cache jq aws-cli

# Copy the shell script to the container
COPY sync_secrets.sh ${APP_ROOT}/sync_secrets.sh

# Command to run the script
CMD [ "./sync_secrets.sh" ]