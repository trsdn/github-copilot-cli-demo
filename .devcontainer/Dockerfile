FROM mcr.microsoft.com/devcontainers/dotnet:8.0

RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get update \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*FROM mcr.microsoft.com/devcontainers/dotnet:8.0

RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace