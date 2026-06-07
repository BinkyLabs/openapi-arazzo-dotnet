// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Writers;

public class OpenApiWriterAnyExtensionsTests
{
    [Fact]
    public void WriteArazzoExtensions_NullExtensions_NoOp()
    {
        using var sw = new StringWriter();
        var writer = new OpenApiJsonWriter(sw);
        writer.WriteStartObject();
        writer.WriteArazzoExtensions(null, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
        Assert.Contains("{", sw.ToString());
    }

    [Fact]
    public void WriteArazzoExtensions_WithExtension_WritesProperty()
    {
        using var sw = new StringWriter();
        var writer = new OpenApiJsonWriter(sw);
        writer.WriteStartObject();
        writer.WriteArazzoExtensions(new Dictionary<string, IArazzoExtension>
        {
            ["x-foo"] = new JsonNodeExtension(JsonNode.Parse("\"bar\"")!)
        }, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();

        var json = JsonNode.Parse(sw.ToString())!;
        Assert.Equal("bar", json["x-foo"]!.GetValue<string>());
    }

    [Fact]
    public void WriteArazzoExtensions_NullValue_WritesNull()
    {
        using var sw = new StringWriter();
        var writer = new OpenApiJsonWriter(sw);
        writer.WriteStartObject();
        writer.WriteArazzoExtensions(new Dictionary<string, IArazzoExtension>
        {
            ["x-empty"] = null!
        }, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();

        var json = JsonNode.Parse(sw.ToString())!;
        Assert.True(json.AsObject().ContainsKey("x-empty"));
        Assert.Null(json["x-empty"]);
    }

    [Fact]
    public void WriteArazzoExtensions_ThrowsOnNullWriter()
    {
        Assert.Throws<ArgumentNullException>(() => Arazzo.Writers.OpenApiWriterAnyExtensions.WriteArazzoExtensions(null!, null, ArazzoSpecVersion.Arazzo1_0));
    }
}