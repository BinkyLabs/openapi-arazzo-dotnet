[![NuGet Version](https://img.shields.io/nuget/vpre/BinkyLabs.OpenApi.Arazzo)](https://www.nuget.org/packages/BinkyLabs.OpenApi.Arazzo) [![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/BinkyLabs/openapi-arazzo-dotnet/dotnet.yml)](https://github.com/BinkyLabs/openapi-arazzo-dotnet/actions/workflows/dotnet.yml)

# OpenAPI Arazzo Library for dotnet

This project provides a .NET implementation of the [OpenAPI Arazzo Specification](https://spec.openapis.org/arazzo/latest.html), allowing you to manage workflows for REST APIs with OpenAPI documents (v3.0+), following the official OpenAPI Arazzo 1.0.0 specification.

The library enables developers to programmatically apply parse, build, serialize, validate arazzo documents.

This library was funded in part by the Interledger Foundation. See the related announcement: [SDK Grant Funds Security Features, OpenAPI Specification, and Arazzo Workflow Support in Kiota](https://interledger.org/news/sdk-grant-funds-security-features-openapi-specification-and-arazzo-workflow-support-kiota).

## Library

### Installing the library

You can install this library via the package explorer or using the following command.

```bash
dotnet add <pathToCsProj> package BinkyLabs.OpenApi.Arazzo
```

### Examples

#### Parsing an Arazzo document

The following example illustrates how you can load or parse an Arazzo document from JSON or YAML.

```csharp
var (arazzoDocument) = await ArazzoDocument.LoadFromUrlAsync("https://source/arazzo.json");
```

## CLI

### Installing the CLI

You can install the Arazzo CLI as a dotnet tool.

```bash
dotnet tool install -g BinkyLabs.OpenApi.Arazzo.Tool
```

### Validating an Arazzo document

The validate command loads an Arazzo document and prints any errors or warnings.

```bash
clia validate pathOrUrlToArazzoDescription
```

Use `--warnings-as-errors` to return an error exit code when warnings are present.

```bash
clia validate pathOrUrlToArazzoDescription --warnings-as-errors
```

### Using the GitHub Action

```yaml
- uses: BinkyLabs/openapi-arazzo-dotnet@v1
  with:
    input: 'arazzo/arazzo.yaml'
    warnings-as-errors: 'true'
```

### Using the container image

```bash
docker run --rm \
  -v $(pwd)/arazzo:/app/arazzo:ro \
  ghcr.io/binkylabs/openapi-arazzo-dotnet:1 \
  validate /app/arazzo/arazzo.yaml
```

#### Serializing an Arazzo document

The following example illustrates how you can serialize an Arazzo document, built by the application or previously parsed, to JSON.

```csharp
var arazzoDocument = new ArazzoDocument
{
    Info = new ArazzoInfo
    {
        Title = "Test Arazzo",
        Version = "1.0.0"
    },
    Workflows = new List<ArazzoWorkflow>
    {
        new ArazzoWorkflow
        {
            //...
        }
    }
};

using var textWriter = new StringWriter();
var writer = new OpenApiJsonWriter(textWriter);
document.SerializeAsV1(writer);
var jsonResult = textWriter.ToString();
// or use flush async if the underlying writer is a stream writer to a file or network stream
```

## Experimental features

This library implements the following experimental features:

- NONE

## Release notes

The OpenAPI Arazzo Libraries releases notes are available from the [CHANGELOG](CHANGELOG.md)

## Debugging

## Contributing

This project welcomes contributions and suggestions.  Make sure you open an issue before sending any pull request to avoid any misunderstanding.

## Trademarks
