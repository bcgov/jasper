FROM mcr.microsoft.com/devcontainers/base:ubuntu

# Install dependencies
RUN apt-get update && \
    apt-get install -y wget tar && \
    apt-get clean

# Set environment variables
ENV S2I_VERSION=source-to-image-v1.4.0-d3544c7e-linux-arm64.tar.gz

# Download and install S2I
RUN wget https://github.com/openshift/source-to-image/releases/download/v1.4.0/${S2I_VERSION} -O /tmp/${S2I_VERSION} && \
    tar -xzf /tmp/${S2I_VERSION} -C /usr/local/bin && \
    rm /tmp/${S2I_VERSION}

# Verify installation
RUN s2i version

# Install .NET SDK
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0

# Set the default command
CMD ["bash"]