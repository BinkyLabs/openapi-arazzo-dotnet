FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
ARG version_suffix
WORKDIR /app

COPY ./src ./clia/src
COPY ./tests ./clia/tests
COPY ./keyfile.snk ./clia/keyfile.snk
COPY ./README.md ./clia/README.md
COPY ./BinkyLabs.OpenAPI.Arazzo.slnx ./clia/BinkyLabs.OpenAPI.Arazzo.slnx
WORKDIR /app/clia
RUN if [ -z "$version_suffix" ]; then \
    dotnet publish ./src/tool/BinkyLabs.OpenApi.Arazzo.Cli.csproj -c Release -p:TreatWarningsAsErrors=false -f net10.0; \
    else \
    dotnet publish ./src/tool/BinkyLabs.OpenApi.Arazzo.Cli.csproj -c Release -p:TreatWarningsAsErrors=false -f net10.0 --version-suffix "$version_suffix"; \
    fi

# Don't use the chiseled image without extras
# (see https://github.com/microsoft/kiota/issues/4600)
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

COPY --from=build-env /app/clia/src/tool/bin/Release/net10.0/publish ./

# Copy sample files for testing
COPY ./debug-samples /app/samples

VOLUME /app/samples
ENV CLIA_CONTAINER=true DOTNET_TieredPGO=1 DOTNET_TC_QuickJitForLoops=1
ENTRYPOINT ["dotnet", "BinkyLabs.OpenApi.Arazzo.Cli.dll"]
LABEL description="# Welcome to OpenAPI Arazzo CLI \
    To start validating Arazzo documents checkout [the documentation](https://github.com/BinkyLabs/openapi-arazzo-dotnet)  \
    [Source dockerfile](https://github.com/BinkyLabs/openapi-arazzo-dotnet/blob/main/Dockerfile)"
LABEL org.opencontainers.image.funding="https://github.com/sponsors/baywet"

