// Licensed under the MIT license.

using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoJsonReaderTests
{
    [Fact]
    public async Task ReadAsync_InvalidJson_AddsErrorAndReturnsNullDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings(), ct);

        Assert.Null(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.NotEmpty(result.Diagnostic!.Errors);
    }

    [Fact]
    public async Task ReadAsync_NullStream_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadAsync(null!, new Uri("https://example.com/"), new ArazzoReaderSettings(), ct));
    }

    [Fact]
    public async Task ReadAsync_NullSettings_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadAsync(stream, new Uri("https://example.com/"), null!, ct));
    }

    [Fact]
    public async Task ReadAsync_UnsupportedSpecVersion_AddsErrorAndReturnsResult()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        var json = """{"arazzo":"99.0.0","info":{"title":"T","version":"1"}}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings(), ct);

        Assert.Null(result.Document);
        Assert.NotEmpty(result.Diagnostic!.Errors);
    }

    [Fact]
    public async Task ReadAsync_ArazzoVersion110_ParsesDocumentAndSetsDiagnosticVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        var json = """
            {
              "arazzo": "1.1.0",
              "info": {
                "title": "T",
                "version": "1"
              },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "workflow1",
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationId": "getWidgets"
                    }
                  ]
                }
              ]
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings(), ct);

        Assert.NotNull(result.Document);
        Assert.Equal("1.1.0", result.Document!.Arazzo);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_1, result.Diagnostic!.SpecificationVersion);
        Assert.Empty(result.Diagnostic.Errors);
    }

    [Fact]
    public async Task ReadAsync_ArazzoVersion110ExtensionParserReceivesSpecVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        ArazzoSpecVersion? parserVersion = null;
        var settings = new ArazzoReaderSettings
        {
            ExtensionParsers = new()
            {
                ["x-test"] = (node, version) =>
                {
                    parserVersion = version;
                    return new JsonNodeExtension(node);
                }
            }
        };
        var json = """
            {
              "arazzo": "1.1.0",
              "info": {
                "title": "T",
                "version": "1"
              },
              "sourceDescriptions": [
                {
                  "name": "source1",
                  "url": "https://example.com/api",
                  "type": "openapi"
                }
              ],
              "workflows": [
                {
                  "workflowId": "workflow1",
                  "steps": [
                    {
                      "stepId": "step1",
                      "operationId": "getWidgets"
                    }
                  ]
                }
              ],
              "x-test": true
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), settings, ct);

        Assert.NotNull(result.Document);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_1, parserVersion);
        Assert.True(result.Document!.Extensions!.ContainsKey("x-test"));
    }

    [Fact]
    public async Task GetJsonNodeFromStreamAsync_ReturnsJsonNode()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"a":1}"""));
        var node = await reader.GetJsonNodeFromStreamAsync(stream, ct);
        Assert.NotNull(node);
        Assert.Equal(1, node!["a"]!.GetValue<int>());
    }
}