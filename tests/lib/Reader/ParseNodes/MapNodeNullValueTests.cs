// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using ParsingContext = BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader.ParseNodes;

public class MapNodeNullValueTests
{
    private static MapNode MakeMap(string json)
    {
        var node = JsonNode.Parse(json)!;
        return new MapNode(new ParsingContext(new ArazzoDiagnostic()), node);
    }

    [Fact]
    public void GetReferencePointer_NullValue_ReturnsNull()
    {
        var map = MakeMap("""{ "$ref": null }""");
        Assert.Null(map.GetReferencePointer());
    }

    [Fact]
    public void GetJsonSchemaIdentifier_NullValue_ReturnsNull()
    {
        var map = MakeMap("""{ "$id": null }""");
        Assert.Null(map.GetJsonSchemaIdentifier());
    }

    [Fact]
    public void GetSummaryValue_NullValue_ReturnsNull()
    {
        var map = MakeMap("""{ "summary": null }""");
        Assert.Null(map.GetSummaryValue());
    }

    [Fact]
    public void GetDescriptionValue_NullValue_ReturnsNull()
    {
        var map = MakeMap("""{ "description": null }""");
        Assert.Null(map.GetDescriptionValue());
    }

    [Fact]
    public void Indexer_NullValue_ReturnsNull()
    {
        var map = MakeMap("""{ "k": null }""");
        Assert.Null(map["k"]);
    }
}