[![NuGet Version](https://img.shields.io/nuget/vpre/BinkyLabs.OpenApi.Arazzo)](https://www.nuget.org/packages/BinkyLabs.OpenApi.Arazzo) [![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/BinkyLabs/openapi-arazzo-dotnet/dotnet.yml)](https://github.com/BinkyLabs/openapi-arazzo-dotnet/actions/workflows/dotnet.yml)

# OpenAPI Arazzo Library for dotnet

This project provides a .NET implementation of the [OpenAPI Arazzo Specification](https://spec.openapis.org/arazzo/latest.html), enabling developers to parse, build, serialize, and validate Arazzo 1.0.x and 1.1.x documents describing workflows across APIs.

This library was funded in part by the Interledger Foundation. See the related announcement: [SDK Grant Funds Security Features, OpenAPI Specification, and Arazzo Workflow Support in Kiota](https://interledger.org/news/sdk-grant-funds-security-features-openapi-specification-and-arazzo-workflow-support-kiota).

## Library

### Arazzo 1.1 support

The object model, readers, and writers support the new Arazzo 1.1 fields and models, including:

- `$self` document identifiers and base URI calculation for relative references.
- AsyncAPI source description types and step fields: `channelPath`, `action`, `correlationId`, `timeout`, and step-level `dependsOn`.
- Selector Objects and typed unions for workflow/step outputs, parameter values, request body payloads, and payload replacement values.
- JSONPath, XPath, and JSON Pointer expression types with type/version compatibility validation.
- `querystring` parameter locations, action-level workflow parameters, and payload replacement `targetSelectorType`.
- Runtime expression syntax for `$message`, `$self`, and workflow input/output references.

AsyncAPI support currently covers the document model and parsing/serialization, not loading or resolving AsyncAPI descriptions. The library does not execute workflows or evaluate runtime expressions, selectors, or conditions. Additional semantic validation and reference-resolution work is tracked in [#115](https://github.com/BinkyLabs/openapi-arazzo-dotnet/issues/115).

### Installing the library

You can install this library via the package explorer or using the following command.

```bash
dotnet add <pathToCsProj> package BinkyLabs.OpenApi.Arazzo
```

### Examples

#### Parsing an Arazzo document

The readers detect Arazzo 1.0.x and 1.1.x documents automatically. The following example loads an Arazzo document from JSON or YAML.

```csharp
var (arazzoDocument, diagnostic) = await ArazzoDocument.LoadFromUrlAsync("https://source/arazzo.json");
```

Inspect `diagnostic.Errors` and `diagnostic.Warnings` before using the parsed document.

#### Serializing an Arazzo 1.1 document

Use `SerializeAsV1_1` to write Arazzo 1.1.0 JSON. `Info.Version` identifies your workflow description; it does not select the Arazzo specification version.

```csharp
using BinkyLabs.OpenApi.Arazzo;
using Microsoft.OpenApi;

var arazzoDocument = new ArazzoDocument
{
    Self = "https://example.com/workflows/pets.arazzo.json",
    Info = new ArazzoInfo
    {
        Title = "Pet workflow",
        Version = "1.0.0"
    },
    SourceDescriptions = new List<ArazzoSourceDescription>
    {
        new ArazzoSourceDescription
        {
            Name = "petStore",
            Url = new Uri("https://petstore3.swagger.io/api/v3/openapi.json"),
            Type = ArazzoDescriptionType.OpenAPI
        }
    },
    Workflows = new List<ArazzoWorkflow>
    {
        new ArazzoWorkflow
        {
            WorkflowId = "getPet",
            Steps = new List<ArazzoStep>
            {
                new ArazzoStep
                {
                    StepId = "fetchPet",
                    OperationId = "getPetById",
                    Parameters = new List<ArazzoParameterUnion>
                    {
                        new ArazzoParameter
                        {
                            Name = "petId",
                            In = ParameterLocation.Path,
                            Value = "1"
                        }
                    },
                    Outputs = new Dictionary<string, ArazzoOutputValueUnion>
                    {
                        ["pet"] = new ArazzoRuntimeExpression("$response.body")
                    }
                }
            }
        }
    }
};

using var textWriter = new StringWriter();
var writer = new OpenApiJsonWriter(textWriter);
arazzoDocument.SerializeAsV1_1(writer);
var jsonResult = textWriter.ToString();
```

Use `SerializeAsV1` when targeting Arazzo 1.0.1. Features without a 1.0 representation may be written as specification extensions or rejected.

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

## Experimental features

This library implements the following experimental features:

- NONE

## Release notes

The OpenAPI Arazzo Libraries releases notes are available from the [CHANGELOG](CHANGELOG.md)

## Debugging

## Contributing

This project welcomes contributions and suggestions.  Make sure you open an issue before sending any pull request to avoid any misunderstanding.

## Trademarks
