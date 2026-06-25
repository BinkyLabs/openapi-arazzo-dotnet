# Docker Support for OpenAPI Arazzo CLI

This document provides instructions for using the OpenAPI Arazzo CLI with Docker.

## Using Pre-built Images

Docker images are automatically built and published to GitHub Container Registry on tagged releases.

```bash
docker pull ghcr.io/binkylabs/openapi-arazzo-dotnet:1
```

## Quick Start with Built-in Samples

```bash
docker build -t clia:latest .
docker run --rm clia:latest validate /app/samples/arazzo.yaml
```

Use `--warnings-as-errors` to return an error exit code when warnings are present.

```bash
docker run --rm clia:latest validate /app/samples/arazzo.yaml --warnings-as-errors
```

## Using Docker Compose

```bash
docker-compose run --rm clia
```

## Running Against Local Files

```bash
docker run --rm \
  -v $(pwd)/examples:/app/examples:ro \
  clia:latest validate /app/examples/arazzo.yaml
```

On Windows PowerShell:

```powershell
docker run --rm `
  -v ${PWD}/examples:/app/examples:ro `
  clia:latest validate /app/examples/arazzo.yaml
```

## Image Details

- Base SDK Image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Runtime Image: `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra`
- Target Framework: .NET 10.0
- Platform: Multi-platform support (linux/amd64, linux/arm64)

## CI/CD Integration

Docker images are built on pull requests and published on tagged releases as part of the CI/CD pipeline.

Images are published to: `ghcr.io/binkylabs/openapi-arazzo-dotnet`
