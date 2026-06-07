// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ParsingContextTests
{
    private static ParsingContext CreateContext() => new(new ArazzoDiagnostic());

    [Fact]
    public void GetFromTempStorage_NoScope_ReturnsDefaultWhenMissing()
    {
        var ctx = CreateContext();

        Assert.Null(ctx.GetFromTempStorage<string>("missing"));
    }

    [Fact]
    public void SetTempStorage_NoScope_StoresAndRetrieves()
    {
        var ctx = CreateContext();
        ctx.SetTempStorage("key", "value");

        Assert.Equal("value", ctx.GetFromTempStorage<string>("key"));
    }

    [Fact]
    public void SetTempStorage_NoScopeNullValue_RemovesKey()
    {
        var ctx = CreateContext();
        ctx.SetTempStorage("key", "value");
        ctx.SetTempStorage("key", null);

        Assert.Null(ctx.GetFromTempStorage<string>("key"));
    }

    [Fact]
    public void SetTempStorage_WithScope_StoresAndRetrieves()
    {
        var ctx = CreateContext();
        var scope = new object();
        ctx.SetTempStorage("key", "value", scope);

        Assert.Equal("value", ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void GetFromTempStorage_UnknownScope_ReturnsDefault()
    {
        var ctx = CreateContext();
        var scope = new object();

        Assert.Null(ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void SetTempStorage_WithScopeNullValue_RemovesKey()
    {
        var ctx = CreateContext();
        var scope = new object();
        ctx.SetTempStorage("key", "value", scope);
        ctx.SetTempStorage("key", null, scope);

        Assert.Null(ctx.GetFromTempStorage<string>("key", scope));
    }

    [Fact]
    public void StartObject_EndObject_AffectsLocation()
    {
        var ctx = CreateContext();
        Assert.Equal("#/", ctx.GetLocation());
        ctx.StartObject("foo");
        ctx.StartObject("bar/baz~qux");
        Assert.Equal("#/foo/bar~1baz~0qux", ctx.GetLocation());
        ctx.EndObject();
        Assert.Equal("#/foo", ctx.GetLocation());
        ctx.EndObject();
        Assert.Equal("#/", ctx.GetLocation());
    }

    [Fact]
    public void PushLoop_AddsKey_ReturnsTrue()
    {
        var ctx = CreateContext();
        Assert.True(ctx.PushLoop("loop1", "a"));
        Assert.True(ctx.PushLoop("loop1", "b"));
    }

    [Fact]
    public void PushLoop_DuplicateKey_ReturnsFalse()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        Assert.False(ctx.PushLoop("loop1", "a"));
    }

    [Fact]
    public void PopLoop_RemovesTop()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        ctx.PushLoop("loop1", "b");
        ctx.PopLoop("loop1");
        Assert.True(ctx.PushLoop("loop1", "b"));
    }

    [Fact]
    public void PopLoop_EmptyStack_DoesNothing()
    {
        var ctx = CreateContext();
        ctx.PushLoop("loop1", "a");
        ctx.PopLoop("loop1");
        ctx.PopLoop("loop1"); // no throw
    }

    [Fact]
    public void Parse_ValidArazzoDocument_ReturnsDocument()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": []
            }
            """)!;

        var doc = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(doc);
        Assert.NotNull(doc.Info);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, ctx.Diagnostic.SpecificationVersion);
    }

    [Fact]
    public void Parse_ArazzoVersion101_ReturnsDocument()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.1",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": []
            }
            """)!;

        var doc = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(doc);
        Assert.NotNull(doc.Info);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, ctx.Diagnostic.SpecificationVersion);
    }

    [Fact]
    public void Parse_MissingInfo_AddsDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "sourceDescriptions": []
            }
            """)!;

        ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("Info is a REQUIRED field"));
    }

    [Fact]
    public void Parse_DuplicateWorkflowParameterNames_AddsDiagnosticError()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            {
              "arazzo": "1.0.0",
              "info": { "title": "T", "version": "1" },
              "sourceDescriptions": [],
              "workflows": [
                {
                  "workflowId": "wf",
                  "parameters": [
                    { "name": "token", "in": "header", "value": "one" },
                    { "name": "token", "in": "query", "value": "two" }
                  ]
                }
              ]
            }
            """)!;

        var document = ctx.Parse(jsonNode, new Uri("https://example.com/"));

        Assert.NotNull(document);
        Assert.Contains(ctx.Diagnostic.Errors, e => e.Message.Contains("duplicate parameter name 'token'", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_MissingVersion_ThrowsOpenApiException()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            { "info": { "title": "T" } }
            """)!;

        Assert.Throws<OpenApiException>(() => ctx.Parse(jsonNode, new Uri("https://example.com/")));
    }

    [Fact]
    public void Parse_UnsupportedVersion_ThrowsUnsupportedSpecException()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""
            { "arazzo": "9.9.9", "info": { "title": "T", "version": "1" }, "sourceDescriptions": [] }
            """)!;

        Assert.Throws<OpenApiUnsupportedSpecVersionException>(() => ctx.Parse(jsonNode, new Uri("https://example.com/")));
    }

    [Fact]
    public void ParseFragment_Arazzo1_0_ReturnsElement()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""{ "title": "T", "version": "1" }""")!;

        var info = ctx.ParseFragment<ArazzoInfo>(jsonNode, ArazzoSpecVersion.Arazzo1_0);

        Assert.NotNull(info);
        Assert.Equal("T", info!.Title);
    }

    [Fact]
    public void ParseFragment_UnsupportedVersion_Throws()
    {
        var ctx = CreateContext();
        var jsonNode = JsonNode.Parse("""{ "title": "T" }""")!;

        Assert.Throws<OpenApiUnsupportedSpecVersionException>(() => ctx.ParseFragment<ArazzoInfo>(jsonNode, (ArazzoSpecVersion)999));
    }
}