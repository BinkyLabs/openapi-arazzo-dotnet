// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;
using BinkyLabs.OpenApi.Arazzo.Reader.V1;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader.V1;

public class ArazzoV1DeserializerErrorTests
{
    [Fact]
    public void ParameterDeserializer_UnknownInValue_RecordsDiagnostic()
    {
        var json = """
            {
              "name": "p",
              "in": "not-a-real-location",
              "value": "v"
            }
            """;
        var jsonNode = JsonNode.Parse(json)!;
        var ctx = new ParsingContext(new ArazzoDiagnostic());
        var parseNode = new MapNode(ctx, jsonNode);

        var parameter = ArazzoV1Deserializer.LoadParameter(parseNode);

        Assert.Null(parameter.In);
        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("not-a-real-location") && e.Message.Contains("not recognized"));
    }

    [Fact]
    public void SourceDescriptionDeserializer_UnknownType_RecordsDiagnostic()
    {
        var json = """
            {
              "name": "s",
              "url": "https://example.com/api",
              "type": "weird-type"
            }
            """;
        var jsonNode = JsonNode.Parse(json)!;
        var ctx = new ParsingContext(new ArazzoDiagnostic());
        var parseNode = new MapNode(ctx, jsonNode);

        ArazzoV1Deserializer.LoadSourceDescription(parseNode);

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("not recognized"));
    }

    [Fact]
    public void LoadExtension_WithMatchingParser_UsesParser()
    {
        var json = """{ "title": "T", "version": "1", "x-custom": "value" }""";
        var jsonNode = JsonNode.Parse(json)!;
        var customExt = new JsonNodeExtension(JsonNode.Parse("\"replaced\"")!);
        var ctx = new ParsingContext(new ArazzoDiagnostic())
        {
            ExtensionParsers = new Dictionary<string, Func<JsonNode, ArazzoSpecVersion, IArazzoExtension>>
            {
                ["x-custom"] = (_, _) => customExt
            }
        };
        var parseNode = new MapNode(ctx, jsonNode);

        var info = ArazzoV1Deserializer.LoadInfo(parseNode);

        Assert.NotNull(info.Extensions);
        Assert.Same(customExt, info.Extensions!["x-custom"]);
    }

    [Fact]
    public void LoadAny_ReturnsUnderlyingJsonNode()
    {
        var json = """{ "k": "v" }""";
        var jsonNode = JsonNode.Parse(json)!;
        var ctx = new ParsingContext(new ArazzoDiagnostic());
        var parseNode = new MapNode(ctx, jsonNode);

        var any = ArazzoV1Deserializer.LoadAny(parseNode);

        Assert.NotNull(any);
        Assert.Equal("v", any["k"]!.GetValue<string>());
    }

    [Fact]
    public void Versionservice_LoadElement_UnknownType_ReturnsDefault()
    {
        var diagnostic = new ArazzoDiagnostic();
        var versionService = new ArazzoV1VersionService(diagnostic);
        var ctx = new ParsingContext(diagnostic);
        var node = ParseNode.Create(ctx, JsonNode.Parse("\"x\"")!);

        // ArazzoStep isn't in the loaders dictionary
        var step = versionService.LoadElement<ArazzoStep>(node);
        Assert.Null(step);
    }

    [Fact]
    public void VersionService_Loaders_ContainsKnownTypes()
    {
        var versionService = new ArazzoV1VersionService(new ArazzoDiagnostic());
        Assert.True(versionService.Loaders.ContainsKey(typeof(ArazzoDocument)));
        Assert.True(versionService.Loaders.ContainsKey(typeof(ArazzoInfo)));
        Assert.True(versionService.Loaders.ContainsKey(typeof(ArazzoWorkflow)));
    }
}