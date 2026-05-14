# PatchPanda.Web - Multi-Stage Docker Build (May 2026 Refined)
# Support: amd64, arm64, arm/v7
# Base OS: Ubuntu 26.04 LTS (Resolute) - Resolves transitive Go CVEs in stdlib 1.26.2

ARG BUILDPLATFORM

# ============================================================================
# STAGE 1: Base Runtime (Hardened Resolute)
# ============================================================================
# Switching from generic :10.0 to :10.0-resolute to get the latest OS security baseline
FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute AS base

WORKDIR /app
EXPOSE 8080

# Patch OS vulnerabilities and install basic dependencies
USER root
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        gnupg \
        lsb-release && \
    rm -rf /var/lib/apt/lists/*
USER app

# ============================================================================
# STAGE 2: Build (Cross-Platform Resolute)
# ============================================================================
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build

ARG TARGETARCH
ARG TARGETVARIANT
WORKDIR /src

# Copy project file and restore specifically for the target architecture
COPY ["PatchPanda.Web/PatchPanda.Web.csproj", "PatchPanda.Web/"]

# Optimization: Unified architecture mapping logic
RUN export DOTNET_ARCH=$(case ${TARGETARCH} in \
    "amd64") echo "x64" ;; \
    "arm64") echo "arm64" ;; \
    "arm") [ "${TARGETVARIANT}" = "v7" ] && echo "arm" || { echo "ERROR: Unsupported ARM variant"; exit 1; } ;; \
    *) echo "ERROR: Unsupported architecture '${TARGETARCH}'"; exit 1 ;; \
    esac) && \
    dotnet restore "PatchPanda.Web/PatchPanda.Web.csproj" \
    --runtime "linux-${DOTNET_ARCH}"

# Copy remaining source code
COPY . .

# Publish the Release binary
RUN export DOTNET_ARCH=$(case ${TARGETARCH} in \
    "amd64") echo "x64" ;; \
    "arm64") echo "arm64" ;; \
    "arm") [ "${TARGETVARIANT}" = "v7" ] && echo "arm" || { echo "ERROR: Unsupported ARM variant"; exit 1; } ;; \
    *) echo "ERROR: Unsupported architecture '${TARGETARCH}'"; exit 1 ;; \
    esac) && \
    dotnet publish "PatchPanda.Web/PatchPanda.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    --runtime "linux-${DOTNET_ARCH}" \
    --no-restore \
    --self-contained false

# ============================================================================
# STAGE 3: Final Production Image (Ubuntu 26.04 LTS "Resolute")
# ============================================================================
FROM base AS final

ARG RELEASE_VERSION
ARG ENABLE_DIAGNOSTICS=0
ENV APP_VERSION=$RELEASE_VERSION
ENV DOTNET_EnableDiagnostics=${ENABLE_DIAGNOSTICS}

# Install Docker CLI using native Resolute repo (Resolves Go transitives in Docker CLI)
USER root
RUN mkdir -m 0755 -p /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    chmod a+r /etc/apt/keyrings/docker.gpg && \
    # Dynamic detection of 'resolute' ensures repo matches base image
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends docker-ce-cli && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy artifacts (owned by root initially for integrity)
COPY --from=build /app/publish .

# Create data directory and fix permissions for non-root 'app' user
RUN mkdir -p /app/data && \
    chown -R app:app /app/data

# Use built-in health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

LABEL version=$RELEASE_VERSION \
    description="PatchPanda Web Application (Hardened Resolute)" \
    maintainer="dkorecko"

USER app
ENTRYPOINT ["dotnet", "PatchPanda.Web.dll"]