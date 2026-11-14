// Licensed under the MIT license.

using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoJsonReaderTests
{
    [Theory]
    [InlineData(null, "https://example.com/cache/arazzo.json")]
    [InlineData("", "https://example.com/cache/arazzo.json")]
    [InlineData("https://example.com/canonical/arazzo.json", "https://example.com/canonical/arazzo.json")]
    [InlineData("../canonical/arazzo.json", "https://example.com/canonical/arazzo.json")]
    [InlineData("canonical/arazzo.json", "https://example.com/cache/canonical/arazzo.json")]
    [InlineData("//canonical.example.com/arazzo.json", "https://canonical.example.com/arazzo.json")]
    [InlineData("?version=2", "https://example.com/cache/arazzo.json?version=2")]
    [InlineData("arazzo%23canonical.json", "https://example.com/cache/arazzo%23canonical.json")]
    public void Read_SelfDeterminesComponentAndSourceDescriptionBaseUri(string? self, string expectedBaseUri)
    {
        foreach (var version in new[] { "1.0.1", "1.1.0" })
        {
            var json = CreateDocumentJson(version);
            if (self is not null)
            {
                json[version == "1.1.0" ? "$self" : "x-$self"] = self;
            }

            var result = new ArazzoJsonReader().Read(json, new Uri("https://example.com/cache/arazzo.json"), new());

            Assert.NotNull(result.Document);
            Assert.NotNull(result.Diagnostic);
            Assert.Empty(result.Diagnostic.Errors);
            Assert.Equal(self, result.Document.Self);
            Assert.Equal(new Uri(expectedBaseUri), result.Document.BaseUri);
            var workspace = result.Document.Workspace;
            Assert.NotNull(workspace);
            Assert.True(workspace.TryGetSourceDescription("api", out var sourceDescription));
            Assert.Equal(new Uri(new Uri(expectedBaseUri), "api.json"), sourceDescription.Uri);
            Assert.NotNull(result.Document.Workflows);
            var input = Assert.IsType<ArazzoInputReference>(result.Document.Workflows[0].Inputs);
            Assert.NotNull(input.Target);
            Assert.Equal(JsonSchemaType.Integer, input.Target.Type);
            Assert.Same(input.Target, workspace.ResolveJsonSchemaReference($"{expectedBaseUri}#/components/inputs/count"));
        }
    }

    [Theory]
    [InlineData("\"https://example.com/arazzo.json#fragment\"", "fragment identifier")]
    [InlineData("\"arazzo.json#\"", "fragment identifier")]
    [InlineData("\"http://[invalid\"", "valid URI-reference")]
    [InlineData("42", "URI-reference string")]
    [InlineData("true", "URI-reference string")]
    [InlineData("{}", "URI-reference string")]
    [InlineData("[]", "URI-reference string")]
    public void Read_InvalidSelf_ReportsLocatedDiagnostic(string value, string expectedMessage)
    {
        foreach (var version in new[] { "1.0.1", "1.1.0" })
        {
            var json = CreateDocumentJson(version);
            var fieldName = version == "1.1.0" ? "$self" : "x-$self";
            json[fieldName] = JsonNode.Parse(value);

            var result = new ArazzoJsonReader().Read(json, new Uri("https://example.com/arazzo.json"), new());

            Assert.NotNull(result.Document);
            Assert.Null(result.Document.Self);
            Assert.NotNull(result.Diagnostic);
            var error = Assert.Single(result.Diagnostic.Errors);
            Assert.Equal($"#/{fieldName}", error.Pointer);
            Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
        }
    }

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

    private static JsonNode CreateDocumentJson(string version)
    {
        var json = JsonNode.Parse("""
            {
              "arazzo": "1.1.0",
              "info": { "title": "Test", "version": "1" },
              "sourceDescriptions": [{ "name": "api", "url": "api.json", "type": "openapi" }],
              "workflows": [{
                "workflowId": "wf",
                "inputs": { "$ref": "#/components/inputs/count" },
                "steps": [{ "stepId": "step", "operationId": "op" }]
              }],
              "components": { "inputs": { "count": { "type": "integer" } } }
            }
            """);
        Assert.NotNull(json);
        json["arazzo"] = version;
        return json;
    }
}