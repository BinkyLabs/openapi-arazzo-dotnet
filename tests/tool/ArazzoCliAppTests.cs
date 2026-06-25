using System;
using System.IO;
using System.Threading.Tasks;

using BinkyLabs.OpenApi.Arazzo.Cli;

namespace BinkyLabs.OpenApi.Arazzo.Cli.Tests;

public sealed class ArazzoCliAppTests : IDisposable
{
    private const string jsonExtension = ".json";
    private const string yamlExtension = ".yaml";
    private readonly string _tempArazzoFileJson = Path.Join(Path.GetTempPath(), Guid.NewGuid() + jsonExtension);
    private readonly string _tempArazzoFileYaml = Path.Join(Path.GetTempPath(), Guid.NewGuid() + yamlExtension);
    private readonly string _tempInvalidArazzoFileJson = Path.Join(Path.GetTempPath(), Guid.NewGuid() + jsonExtension);

    private readonly string _validArazzoJson =
        """
        {
            "arazzo": "1.0.1",
            "info": {
                "title": "Test Arazzo",
                "version": "1.0.0"
            },
            "sourceDescriptions": [
                {
                    "name": "testApi",
                    "url": "https://example.com/openapi.json",
                    "type": "openapi"
                }
            ],
            "workflows": [
                {
                    "workflowId": "testWorkflow",
                    "steps": [
                        {
                            "stepId": "testStep",
                            "operationId": "getTest"
                        }
                    ]
                }
            ]
        }
        """;

    private readonly string _validArazzoYaml =
        """
        arazzo: 1.0.1
        info:
          title: Test Arazzo
          version: 1.0.0
        sourceDescriptions:
          - name: testApi
            url: https://example.com/openapi.json
            type: openapi
        workflows:
          - workflowId: testWorkflow
            steps:
              - stepId: testStep
                operationId: getTest
        """;

    private readonly string _invalidArazzoJson =
        """
        {
            "arazzo": "1.0.1"
        }
        """;

    public ArazzoCliAppTests()
    {
        File.WriteAllText(_tempArazzoFileJson, _validArazzoJson);
        File.WriteAllText(_tempArazzoFileYaml, _validArazzoYaml);
        File.WriteAllText(_tempInvalidArazzoFileJson, _invalidArazzoJson);
    }

    public void Dispose()
    {
        if (File.Exists(_tempArazzoFileJson)) File.Delete(_tempArazzoFileJson);
        if (File.Exists(_tempArazzoFileYaml)) File.Delete(_tempArazzoFileYaml);
        if (File.Exists(_tempInvalidArazzoFileJson)) File.Delete(_tempInvalidArazzoFileJson);
    }

    [Fact]
    public async Task RunAsync_ValidateValidArazzoJson_ReturnsOK()
    {
        var result = await ArazzoCliApp.RunAsync(["validate", _tempArazzoFileJson], TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task RunAsync_ValidateValidArazzoYamlWithWarningsAsErrors_ReturnsOK()
    {
        var result = await ArazzoCliApp.RunAsync(["validate", _tempArazzoFileYaml, "--warnings-as-errors"], TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task RunAsync_ValidateInvalidArazzo_ReturnsError()
    {
        var result = await ArazzoCliApp.RunAsync(["validate", _tempInvalidArazzoFileJson], TestContext.Current.CancellationToken);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task RunAsync_MissingArguments_ReturnsError()
    {
        var result = await ArazzoCliApp.RunAsync([], TestContext.Current.CancellationToken);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task RunAsync_MissingFile_ReturnsError()
    {
        var result = await ArazzoCliApp.RunAsync(["validate", Path.Join(Path.GetTempPath(), Guid.NewGuid() + jsonExtension)], TestContext.Current.CancellationToken);

        Assert.Equal(1, result);
    }
}