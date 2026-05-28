// Licensed under the MIT license.

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Extensions;

public class ArazzoExtensibleExtensionsTests
{
    [Fact]
    public void AddExtension_AddsToDictionary()
    {
        var doc = new ArazzoDocument();
        var ext = new JsonNodeExtension(JsonNode.Parse("\"v\"")!);

        doc.AddExtension("x-foo", ext);

        Assert.NotNull(doc.Extensions);
        Assert.Same(ext, doc.Extensions!["x-foo"]);
    }

    [Fact]
    public void AddExtension_PreservesExistingExtensions()
    {
        var doc = new ArazzoDocument
        {
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-existing"] = new JsonNodeExtension(JsonNode.Parse("1")!)
            }
        };

        doc.AddExtension("x-new", new JsonNodeExtension(JsonNode.Parse("2")!));

        Assert.True(doc.Extensions.ContainsKey("x-existing"));
        Assert.True(doc.Extensions.ContainsKey("x-new"));
    }

    [Fact]
    public void AddExtension_ThrowsWhenNameDoesNotStartWithXPrefix()
    {
        var doc = new ArazzoDocument();
        var ext = new JsonNodeExtension(JsonNode.Parse("\"v\"")!);

        var ex = Assert.Throws<OpenApiException>(() => doc.AddExtension("foo", ext));
        Assert.Contains("must start with x-", ex.Message);
    }

    [Fact]
    public void AddExtension_ThrowsOnNullArgs()
    {
        var doc = new ArazzoDocument();
        var ext = new JsonNodeExtension(JsonNode.Parse("\"v\"")!);

        Assert.Throws<ArgumentNullException>(() => ((ArazzoDocument)null!).AddExtension("x-foo", ext));
        Assert.Throws<ArgumentException>(() => doc.AddExtension("", ext));
        Assert.Throws<ArgumentNullException>(() => doc.AddExtension("x-foo", null!));
    }
}