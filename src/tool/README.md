# CLIA - CLI Arazzo

The BinkyLabs CLI Arazzo is a dotnet tool which can be used to validate OpenAPI Arazzo descriptions.

## Installation

```shell
dotnet tool install -g BinkyLabs.OpenApi.Arazzo.Tool
```

## Usage

### Validate an Arazzo description

The validate command loads an Arazzo document and prints any errors or warnings.

```shell
clia validate pathOrUrlToArazzoDescription
```

Use `--warnings-as-errors` to return an error exit code when warnings are present.

```shell
clia validate pathOrUrlToArazzoDescription --warnings-as-errors
```
