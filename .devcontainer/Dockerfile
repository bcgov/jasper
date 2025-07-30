FROM mcr.microsoft.com/devcontainers/base:ubuntu

# Install dependencies
RUN apt-get update && \
    apt-get install -y \
    curl \
    tar \
    wget && \
    apt-get clean

# ==========================================================================================================================
# Install S2I
# --------------------------------------------------------------------------------------------------------------------------
ENV S2I_VERSION=source-to-image-v1.4.0-d3544c7e-linux-arm64.tar.gz
RUN wget https://github.com/openshift/source-to-image/releases/download/v1.4.0/${S2I_VERSION} -O /tmp/${S2I_VERSION} && \
    tar -xzf /tmp/${S2I_VERSION} -C /usr/local/bin && \
    rm /tmp/${S2I_VERSION}

# Verify installation
RUN s2i version
# ==========================================================================================================================

# ==========================================================================================================================
# Install AWS Session Manager Plugin
# --------------------------------------------------------------------------------------------------------------------------
RUN curl "https://s3.amazonaws.com/session-manager-downloads/plugin/latest/ubuntu_64bit/session-manager-plugin.deb" -o "session-manager-plugin.deb" && \
    dpkg -i session-manager-plugin.deb && \
    rm session-manager-plugin.deb
# ==========================================================================================================================

# ==========================================================================================================================
# Install Terraform CLI
# --------------------------------------------------------------------------------------------------------------------------
# 
RUN apt-get update && apt-get install -y unzip curl gnupg software-properties-common

ARG TERRAFORM_VERSION=1.12.2
RUN curl -fsSL https://releases.hashicorp.com/terraform/${TERRAFORM_VERSION}/terraform_${TERRAFORM_VERSION}_linux_amd64.zip -o terraform.zip && \
    unzip terraform.zip && mv terraform /usr/local/bin/ && rm terraform.zip
# ==========================================================================================================================

# Set the default command
CMD ["bash"]
