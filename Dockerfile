# PatchPanda.Web - Production-Grade Multi-Stage Build
# Target Architectures: amd64, arm64, arm/v7
# Base OS: Ubuntu 26.04 LTS (Resolute)
# Security: Non-root execution, minimal attack surface, atomic layer generation

ARG BUILDPLATFORM

# ============================================================================
# STAGE 1: Base Runtime
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute AS base

WORKDIR /app
EXPOSE 8080

# Install ONLY the necessary runtime dependencies.
# Note: Security updates handled by the -resolute tag.
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

USER app

# ============================================================================
# STAGE 2: Cross-Platform Build Environment
# ============================================================================
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build

ARG TARGETARCH
ARG TARGETVARIANT
WORKDIR /src

# Leverage layer caching by restoring dependencies before the full source copy
COPY ["PatchPanda.Web/PatchPanda.Web.csproj", "PatchPanda.Web/"]

# Map Docker architecture to .NET RID, with strict validation for ARMv7
RUN export DOTNET_ARCH=$(case ${TARGETARCH} in \
        "amd64") echo "x64" ;; \
        "arm64") echo "arm64" ;; \
        "arm") [ "${TARGETVARIANT}" = "v7" ] && echo "arm" || { echo "ERROR: Unsupported ARM variant"; exit 1; } ;; \
        *) echo "ERROR: Unsupported architecture '${TARGETARCH}'"; exit 1 ;; \
    esac) && \
    dotnet restore "PatchPanda.Web/PatchPanda.Web.csproj" \
    --runtime "linux-${DOTNET_ARCH}"

# Build and publish framework-dependent binaries
COPY . .
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
# STAGE 3: Final Production Assembly
# ============================================================================
FROM base AS final

ARG RELEASE_VERSION
ARG ENABLE_DIAGNOSTICS=0
ENV AppVersion=$RELEASE_VERSION
ENV DOTNET_EnableDiagnostics=${ENABLE_DIAGNOSTICS}

# Elevate to configure system tools and directories
USER root

# Temporarily install gnupg to fetch the repository key, install docker-cli, 
# and then aggressively purge gnupg to minimize the final attack surface.
RUN apt-get update && \
    apt-get install -y --no-install-recommends gnupg && \
    install -m 0755 -d /etc/apt/keyrings && \
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && \
    chmod a+r /etc/apt/keyrings/docker.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu resolute stable" > /etc/apt/sources.list.d/docker.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends docker-ce-cli && \
    apt-get purge -y gnupg && \
    apt-get autoremove -y && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Transfer application binaries with native ownership allocation
COPY --from=build --chown=app:app /app/publish .

# Atomic Directory Creation
# Replaces separate 'mkdir' and 'chown' commands. This creates the folder and 
# sets exact permissions/ownership in a single native Linux operation.
RUN install -d -m 0755 -o app -g app /app/data

# Application health monitoring
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

LABEL version=$RELEASE_VERSION \
    description="PatchPanda Web Application (Hardened Resolute)"

# Enforce least-privilege execution
USER app
ENTRYPOINT ["dotnet", "PatchPanda.Web.dll"]
